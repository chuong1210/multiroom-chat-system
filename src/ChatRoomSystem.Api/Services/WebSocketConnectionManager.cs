using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatRoomSystem.Shared.Models;

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

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add connection mới
    /// </summary>
    public void AddConnection(string userId, WebSocket socket, string username)
    {
        _connections[userId] = socket;
        _userInfo[userId] = new UserConnectionInfo
        {
            UserId = userId,
            Username = username,
            ConnectedAt = DateTime.UtcNow
        };

        _logger.LogInformation($"User {username} ({userId}) connected. Total connections: {_connections.Count}");
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
                users.Remove(userId);
            }
        }

        _logger.LogInformation($"User {userId} disconnected. Total connections: {_connections.Count}");
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

        _logger.LogInformation($"User {userId} joined room {roomId}. Room members: {users.Count}");
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

            _logger.LogInformation($"User {userId} left room {roomId}. Room members: {users.Count}");
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
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(bytes);

            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to user {userId}");
            await RemoveConnectionAsync(userId);
        }
    }

    /// <summary>
    /// Broadcast message đến tất cả users trong room
    /// </summary>
    public async Task BroadcastToRoomAsync(string roomId, WebSocketMessage message, string? excludeUserId = null)
    {
        var userIds = GetUsersInRoom(roomId);

        var tasks = userIds
            .Where(uid => uid != excludeUserId)
            .Select(uid => SendMessageAsync(uid, message));

        await Task.WhenAll(tasks);

        _logger.LogDebug($"Broadcasted message type {message.Type} to room {roomId} ({userIds.Count} users)");
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

        _logger.LogDebug($"Broadcasted message type {message.Type} to all users ({_connections.Count} users)");
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
