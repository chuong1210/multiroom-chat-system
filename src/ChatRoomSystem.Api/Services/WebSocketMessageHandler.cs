using System.Text.Json;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

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
        _logger.LogDebug($"Handling message type {message.Type} from user {userId}");

        try
        {
            switch (message.Type)
            {
                case WebSocketMessageType.Ping:
                    await HandlePingAsync(userId, connectionManager);
                    break;

                case WebSocketMessageType.JoinRoom:
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

                case WebSocketMessageType.TaskCreated:
                case WebSocketMessageType.TaskUpdated:
                case WebSocketMessageType.TaskDeleted:
                    await HandleTaskUpdateAsync(userId, message, connectionManager);
                    break;

                case WebSocketMessageType.WhiteboardDraw:
                    await HandleWhiteboardDrawAsync(userId, message.Data, connectionManager);
                    break;

                case WebSocketMessageType.WhiteboardClear:
                    await HandleWhiteboardClearAsync(userId, message.Data, connectionManager);
                    break;

                case WebSocketMessageType.WebRTCOffer:
                case WebSocketMessageType.WebRTCAnswer:
                case WebSocketMessageType.WebRTCIceCandidate:
                    await HandleWebRTCSignalingAsync(userId, message, connectionManager);
                    break;

                case WebSocketMessageType.CallInitiate:
                case WebSocketMessageType.CallAccept:
                case WebSocketMessageType.CallReject:
                case WebSocketMessageType.CallEnd:
                    await HandleCallSignalingAsync(userId, message, connectionManager);
                    break;

                case WebSocketMessageType.ScreenShareStart:
                case WebSocketMessageType.ScreenShareStop:
                    await HandleScreenShareAsync(userId, message, connectionManager);
                    break;

                default:
                    _logger.LogWarning($"Unknown message type: {message.Type}");
                    await connectionManager.SendMessageAsync(userId, new WebSocketMessage
                    {
                        Type = WebSocketMessageType.Error,
                        Data = JsonSerializer.Serialize(new ErrorPayload
                        {
                            Message = $"Unknown message type: {message.Type}",
                            Code = "UNKNOWN_MESSAGE_TYPE"
                        })
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling message type {message.Type} from user {userId}");
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

    /// <summary>
    /// Handle Ping message (heartbeat)
    /// </summary>
    private async Task HandlePingAsync(string userId, WebSocketConnectionManager connectionManager)
    {
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.Pong
        });
    }

    /// <summary>
    /// Handle Join Room message
    /// </summary>
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

        // Check if user is member của room
        var isMember = await dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == payload.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            await SendErrorAsync(userId, "You are not a member of this room", "NOT_MEMBER", connectionManager);
            return;
        }

        // Add user vào room trong connection manager
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

        _logger.LogInformation($"User {userId} joined room {payload.RoomId}");
    }

    /// <summary>
    /// Handle Leave Room message
    /// </summary>
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

        // Remove user khỏi room
        connectionManager.RemoveUserFromRoom(userId, payload.RoomId);

        // Send confirmation
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = WebSocketMessageType.RoomLeft,
            Data = JsonSerializer.Serialize(new { roomId = payload.RoomId })
        });

        // Notify other users
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

        _logger.LogInformation($"User {userId} left room {payload.RoomId}");
    }

    /// <summary>
    /// Handle Chat Message
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

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();

        // Verify user is member
        var isMember = await dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == payload.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            await SendErrorAsync(userId, "You are not a member of this room", "NOT_MEMBER", connectionManager);
            return;
        }

        // Get user info
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
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

        // Broadcast message đến all users trong room
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

        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = WebSocketMessageType.MessageReceived,
                Data = JsonSerializer.Serialize(messageDto)
            });

        _logger.LogInformation($"Message sent by user {userId} to room {payload.RoomId}");
    }

    /// <summary>
    /// Handle Typing indicator
    /// </summary>
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

    /// <summary>
    /// Handle Task updates (broadcast to room)
    /// </summary>
    private async Task HandleTaskUpdateAsync(
        string userId,
        WebSocketMessage message,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(message.Data))
        {
            return;
        }

        // Parse để get roomId
        var taskData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
        if (taskData == null || !taskData.ContainsKey("roomId"))
        {
            return;
        }

        var roomId = taskData["roomId"].GetString();
        if (string.IsNullOrEmpty(roomId))
        {
            return;
        }

        // Broadcast task update đến all users trong room
        await connectionManager.BroadcastToRoomAsync(
            roomId,
            message,
            excludeUserId: userId);
    }

    /// <summary>
    /// Handle Whiteboard Draw
    /// </summary>
    private async Task HandleWhiteboardDrawAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<WhiteboardPayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId))
        {
            return;
        }

        // Broadcast drawing data đến all users trong room
        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = WebSocketMessageType.WhiteboardData,
                Data = data
            },
            excludeUserId: userId);
    }

    /// <summary>
    /// Handle Whiteboard Clear
    /// </summary>
    private async Task HandleWhiteboardClearAsync(
        string userId,
        string? data,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<WhiteboardPayload>(data);
        if (payload == null || string.IsNullOrEmpty(payload.RoomId))
        {
            return;
        }

        // Broadcast clear command
        await connectionManager.BroadcastToRoomAsync(
            payload.RoomId,
            new WebSocketMessage
            {
                Type = WebSocketMessageType.WhiteboardClear,
                Data = data
            },
            excludeUserId: userId);
    }

    /// <summary>
    /// Handle WebRTC Signaling (Offer/Answer/ICE Candidate)
    /// </summary>
    private async Task HandleWebRTCSignalingAsync(
        string userId,
        WebSocketMessage message,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(message.Data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<WebRTCSignalPayload>(message.Data);
        if (payload == null || string.IsNullOrEmpty(payload.ToUserId))
        {
            return;
        }

        // Forward signaling message đến target user
        await connectionManager.SendMessageAsync(payload.ToUserId, message);
    }

    /// <summary>
    /// Handle Call Signaling (Initiate/Accept/Reject/End)
    /// </summary>
    private async Task HandleCallSignalingAsync(
        string userId,
        WebSocketMessage message,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(message.Data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<WebRTCSignalPayload>(message.Data);
        if (payload == null)
        {
            return;
        }

        // If ToUserId specified, send to specific user
        if (!string.IsNullOrEmpty(payload.ToUserId))
        {
            await connectionManager.SendMessageAsync(payload.ToUserId, message);
        }
        // If RoomId specified, broadcast to room
        else if (!string.IsNullOrEmpty(payload.RoomId))
        {
            await connectionManager.BroadcastToRoomAsync(payload.RoomId, message, excludeUserId: userId);
        }
    }

    /// <summary>
    /// Handle Screen Share events
    /// </summary>
    private async Task HandleScreenShareAsync(
        string userId,
        WebSocketMessage message,
        WebSocketConnectionManager connectionManager)
    {
        if (string.IsNullOrEmpty(message.Data))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
        if (payload == null || !payload.ContainsKey("roomId"))
        {
            return;
        }

        var roomId = payload["roomId"].GetString();
        if (string.IsNullOrEmpty(roomId))
        {
            return;
        }

        // Broadcast screen share event
        await connectionManager.BroadcastToRoomAsync(roomId, message, excludeUserId: userId);
    }

    /// <summary>
    /// Send error message to user
    /// </summary>
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
