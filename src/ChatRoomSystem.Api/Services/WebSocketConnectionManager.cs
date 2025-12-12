// ChatRoomSystem.Api/Services/WebSocketConnectionManager.cs
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatRoomSystem.Data;
using ChatRoomSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;
using CustomWebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;
using NetWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace ChatRoomSystem.Api.Services;

/// <summary>
/// Service quản lý tất cả WebSocket connections
/// Sử dụng ConcurrentDictionary để thread-safe operations
/// </summary>
public class WebSocketConnectionManager
{
    // Map userId -> WebSocket connection
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    // Map userId -> user info (username, etc.)
    private readonly ConcurrentDictionary<string, UserConnectionInfo> _userInfo = new();

    // Map roomId -> List of userIds (users currently in the room)
    private readonly ConcurrentDictionary<string, HashSet<string>> _roomUsers = new();

    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Add connection mới
    /// </summary>
    public async Task AddConnectionAsync(string userId, WebSocket socket, string username)
    {
        _connections[userId] = socket;
        _userInfo[userId] = new UserConnectionInfo
        {
            UserId = userId,
            Username = username,
            ConnectedAt = DateTime.UtcNow
        };

        // ✅ Update User.IsOnline in database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();

            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"✅ User {username} ({userId}) marked as online in database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to update online status for user {userId}");
        }

        _logger.LogInformation($"✅ User {username} ({userId}) connected. Total connections: {_connections.Count}");
    }

    /// <summary>
    /// Add connection mới (sync version for backward compatibility)
    /// </summary>
    public void AddConnection(string userId, WebSocket socket, string username)
    {
        _ = AddConnectionAsync(userId, socket, username);
    }

    /// <summary>
    /// Remove connection
    /// </summary>
    public async Task RemoveConnectionAsync(string userId)
    {
        if (_connections.TryRemove(userId, out var socket))
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error closing socket for user {userId}");
                }
            }

            socket.Dispose();
        }

        _userInfo.TryRemove(userId, out _);

        // Remove user từ tất cả rooms
        foreach (var roomId in _roomUsers.Keys.ToList())
        {
            if (_roomUsers.TryGetValue(roomId, out var users))
            {
                lock (users)
                {
                    users.Remove(userId);
                }
            }
        }

        // ✅ Update User.IsOnline in database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatRoomDbContext>();

            var user = await dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"👋 User {userId} marked as offline in database");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to update offline status for user {userId}");
        }

        _logger.LogInformation($"👋 User {userId} disconnected. Total connections: {_connections.Count}");
    }

    /// <summary>
    /// Get connection by userId
    /// </summary>
    public WebSocket? GetConnection(string userId)
    {
        _connections.TryGetValue(userId, out var socket);
        return socket;
    }

    /// <summary>
    /// Check if user is connected
    /// </summary>
    public bool IsUserOnline(string userId)
    {
        return _connections.ContainsKey(userId);
    }

    /// <summary>
    /// Add user vào room
    /// </summary>
    public void AddUserToRoom(string userId, string roomId)
    {
        var users = _roomUsers.GetOrAdd(roomId, _ => new HashSet<string>());
        lock (users)
        {
            users.Add(userId);
        }

        _logger.LogInformation($"📥 User {userId} joined room {roomId}. Room members: {users.Count}");
    }

    /// <summary>
    /// Remove user khỏi room
    /// </summary>
    public void RemoveUserFromRoom(string userId, string roomId)
    {
        if (_roomUsers.TryGetValue(roomId, out var users))
        {
            lock (users)
            {
                users.Remove(userId);
            }

            _logger.LogInformation($"📤 User {userId} left room {roomId}. Room members: {users.Count}");
        }
    }

    /// <summary>
    /// Get tất cả users trong room
    /// </summary>
    public List<string> GetUsersInRoom(string roomId)
    {
        if (_roomUsers.TryGetValue(roomId, out var users))
        {
            lock (users)
            {
                return users.ToList();
            }
        }

        return new List<string>();
    }

    /// <summary>
    /// Send message đến một user cụ thể
    /// </summary>
    public async Task SendMessageAsync(string userId, WebSocketMessage message)
    {
        var socket = GetConnection(userId);
        if (socket == null || socket.State != WebSocketState.Open)
        {
            _logger.LogWarning($"⚠️ Cannot send message to user {userId}: socket not available or not open");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(bytes);

            await socket.SendAsync(buffer, NetWebSocketMessageType.Text, true, CancellationToken.None);

            _logger.LogDebug($"✉️ Sent message type {message.Type} to user {userId}");
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogWarning($"⚠️ WebSocket closed prematurely for user {userId}");
            await RemoveConnectionAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔴 Error sending message to user {userId}");
            await RemoveConnectionAsync(userId);
        }
    }

    /// <summary>
    /// ✅ Send message đến một user cụ thể (alias method for consistency)
    /// </summary>
    public async Task SendToUserAsync(string userId, WebSocketMessage message)
    {
        await SendMessageAsync(userId, message);
    }

    /// <summary>
    /// Broadcast message đến tất cả users trong room
    /// </summary>
    public async Task BroadcastToRoomAsync(string roomId, WebSocketMessage message, string? excludeUserId = null)
    {
        var userIds = GetUsersInRoom(roomId);

        _logger.LogInformation($"📢 Broadcasting message type {message.Type} to room {roomId} ({userIds.Count} users)");

        var tasks = new List<Task>();
        foreach (var uid in userIds)
        {
            if (uid != excludeUserId)
            {
                tasks.Add(SendMessageAsync(uid, message));
            }
        }

        await Task.WhenAll(tasks);

        _logger.LogDebug($"✅ Broadcast complete to room {roomId}");
    }

    /// <summary>
    /// Broadcast message đến tất cả connected users
    /// </summary>
    public async Task BroadcastToAllAsync(WebSocketMessage message, string? excludeUserId = null)
    {
        var tasks = _connections.Keys
            .Where(uid => uid != excludeUserId)
            .Select(uid => SendMessageAsync(uid, message));

        await Task.WhenAll(tasks);

        _logger.LogDebug($"📢 Broadcasted message type {message.Type} to all users ({_connections.Count} users)");
    }

    /// <summary>
    /// Get all online user IDs
    /// </summary>
    public List<string> GetOnlineUserIds()
    {
        return _connections.Keys.ToList();
    }

    /// <summary>
    /// Get user info
    /// </summary>
    public UserConnectionInfo? GetUserInfo(string userId)
    {
        _userInfo.TryGetValue(userId, out var info);
        return info;
    }

    /// <summary>
    /// Get total connection count
    /// </summary>
    public int GetConnectionCount()
    {
        return _connections.Count;
    }

    /// <summary>
    /// Get room member count
    /// </summary>
    public int GetRoomMemberCount(string roomId)
    {
        return GetUsersInRoom(roomId).Count;
    }
}

/// <summary>
/// Info về user connection
/// </summary>
public class UserConnectionInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
}