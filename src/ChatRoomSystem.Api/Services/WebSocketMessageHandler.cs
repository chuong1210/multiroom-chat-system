using System.Text.Json;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;
using WebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;

namespace ChatRoomSystem.Api.Services;

/// <summary>
/// Service xử lý tất cả WebSocket messages
/// </summary>
public class WebSocketMessageHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebSocketMessageHandler> _logger;

    public WebSocketMessageHandler(
        IServiceProvider serviceProvider,
        ILogger<WebSocketMessageHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Handle incoming message từ client
    /// </summary>
    public async Task HandleMessageAsync(
        string userId,
        WebSocketMessage message,
        WebSocketConnectionManager connectionManager)
    {
        _logger.LogInformation($"📨 Handling message type {message.Type} from user {userId}");

        try
        {
            switch (message.Type)
            {
                case WebSocketMessageType.Ping:
                    await HandlePingAsync(userId, connectionManager);
                    break;

                case WebSocketMessageType.JoinRoomRequest:
                    await HandleJoinRoomAsync(userId, message.Data, connectionManager);
                    break;

                case WebSocketMessageType.LeaveRoom:
                    await HandleLeaveRoomAsync(userId, message.Data, connectionManager);
                    break;

                case WebSocketMessageType.ChatMessage:
                    await HandleChatMessageAsync(userId, message.Data, connectionManager);
                    break;

                case WebSocketMessageType.UserTyping:
                    await HandleTypingAsync(userId, message.Data, connectionManager, true);
                    break;

                case WebSocketMessageType.UserStoppedTyping:
                    await HandleTypingAsync(userId, message.Data, connectionManager, false);
                    break;

                default:
                    _logger.LogWarning($"⚠️ Unknown message type: {message.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔴 Error handling message type {message.Type} from user {userId}");
            await connectionManager.SendMessageAsync(userId, new WebSocketMessage
            {
                Type = WebSocketMessageType.Error,
                Data = JsonSerializer.Serialize(new ErrorPayload
                {
                    Message = "Error processing message",
                    Code = "PROCESSING_ERROR"
                })
            });
        }
    }

    #region Message Handlers

    private async Task HandlePingAsync(string userId, WebSocketConnectionManager connectionManager)
    {
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.Pong
        });
    }

    private async Task HandleJoinRoomAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(data))
        {
            await SendErrorAsync(userId, "Room ID required", "ROOM_ID_REQUIRED", connectionManager);
            return;
        }

        var payload = JsonSerializer.Deserialize<JoinRoomPayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId))
        {
            await SendErrorAsync(userId, "Invalid join room payload", "INVALID_PAYLOAD", connectionManager);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();

        // Check if user is member
        var isMember = await dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == payload.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            await SendErrorAsync(userId, "You are not a member of this room", "NOT_MEMBER", connectionManager);
            return;
        }

        // Add user to room
        connectionManager.AddUserToRoom(userId, payload.RoomId);

        // Send confirmation
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.RoomJoined,
            Data = JsonSerializer.Serialize(new { roomId = payload.RoomId })
        });

        // Notify other users
        var userInfo = connectionManager.GetUserInfo(userId);
        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = WebSocketMessageType.UserJoinedRoom,
                Data = JsonSerializer.Serialize(new
                {
                    roomId = payload.RoomId,
                    userId,
                    username = userInfo?.Username ?? "Unknown"
                })
            },
            excludeUserId: userId);

        _logger.LogInformation($"✅ User {userId} joined room {payload.RoomId}");
    }

    private async Task HandleLeaveRoomAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<JoinRoomPayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId))
        {
            return;
        }

        connectionManager.RemoveUserFromRoom(userId, payload.RoomId);

        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.RoomLeft,
            Data = JsonSerializer.Serialize(new { roomId = payload.RoomId })
        });

        var userInfo = connectionManager.GetUserInfo(userId);
        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = WebSocketMessageType.UserLeftRoom,
                Data = JsonSerializer.Serialize(new
                {
                    roomId = payload.RoomId,
                    userId,
                    username = userInfo?.Username ?? "Unknown"
                })
            });

        _logger.LogInformation($"👋 User {userId} left room {payload.RoomId}");
    }

    /// <summary>
    /// ✅ FIX: Handle Chat Message - Broadcast to ALL users including sender
    /// </summary>
    private async Task HandleChatMessageAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(data))
        {
            await SendErrorAsync(userId, "Message data required", "MESSAGE_REQUIRED", connectionManager);
            return;
        }

        var payload = JsonSerializer.Deserialize<ChatMessagePayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId) || string.IsNullOrEmpty(payload.Content))
        {
            await SendErrorAsync(userId, "Invalid message payload", "INVALID_PAYLOAD", connectionManager);
            return;
        }

        _logger.LogInformation($"💬 Processing chat message from {userId} to room {payload.RoomId}: {payload.Content}");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();

        // Verify user is member
        var isMember = await dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == payload.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            _logger.LogWarning($"⚠️ User {userId} is not a member of room {payload.RoomId}");
            await SendErrorAsync(userId, "You are not a member of this room", "NOT_MEMBER", connectionManager);
            return;
        }

        // Get user info
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogError($"🔴 User {userId} not found in database");
            return;
        }

        // Save message to database
        var message = new Message
        {
            RoomId = payload.RoomId,
            SenderId = userId,
            Content = payload.Content,
            Type = payload.MessageType,
            Timestamp = DateTime.UtcNow
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation($"💾 Message saved to database: {message.Id}");

        // Create DTO
        var messageDto = new MessageDto
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = userId,
            SenderUsername = user.UserName ?? user.DisplayName,
            Content = message.Content,
            Type = message.Type,
            Timestamp = message.Timestamp
        };

        // ✅ FIX: Broadcast to ALL users in room (including sender)
        var broadcastMessage = new WebSocketMessage
        {
            Type = WebSocketMessageType.MessageReceived,
            Data = JsonSerializer.Serialize(messageDto)
        };

        var usersInRoom = connectionManager.GetUsersInRoom(payload.RoomId);
        _logger.LogInformation($"📢 Broadcasting message to {usersInRoom.Count} users in room {payload.RoomId}");

        foreach (var targetUserId in usersInRoom)
        {
            try
            {
                await connectionManager.SendMessageAsync(targetUserId, broadcastMessage);
                _logger.LogDebug($"✉️ Sent message to user {targetUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔴 Failed to send message to user {targetUserId}");
            }
        }

        _logger.LogInformation($"✅ Message broadcast complete for room {payload.RoomId}");
    }

    private async Task HandleTypingAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager,
        bool isTyping)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<TypingPayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId))
        {
            return;
        }

        var userInfo = connectionManager.GetUserInfo(userId);

        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = isTyping ? WebSocketMessageType.UserTyping : WebSocketMessageType.UserStoppedTyping,
                Data = JsonSerializer.Serialize(new TypingPayload
                {
                    RoomId = payload.RoomId,
                    UserId = userId,
                    Username = userInfo?.Username ?? "Unknown"
                })
            },
            excludeUserId: userId);
    }

    private async Task SendErrorAsync(
        string userId,
        string message,
        string code,
        WebSocketConnectionManager connectionManager)
    {
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.Error,
            Data = JsonSerializer.Serialize(new ErrorPayload
            {
                Message = message,
                Code = code
            })
        });
    }

    #endregion
}