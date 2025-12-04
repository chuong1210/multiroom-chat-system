using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatRoomSystem.Shared.Models;
using CustomWebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;
using NetWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace ChatRoomSystem.Client.Services;

/// <summary>
/// Service quản lý WebSocket connection từ Blazor client đến backend
/// </summary>
public class WebSocketService : IAsyncDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _baseUrl;
    private readonly ILogger<WebSocketService> _logger;

    // Events cho các message types
    public event Action? OnConnected;
    public event Action<string>? OnDisconnected;
    public event Action<MessageDto>? OnMessageReceived;
    public event Action<string, string>? OnUserJoinedRoom;
    public event Action<string, string>? OnUserLeftRoom;
    public event Action<string, string>? OnUserOnline;
    public event Action<string>? OnUserOffline;
    public event Action<TypingPayload>? OnUserTyping;
    public event Action<TypingPayload>? OnUserStoppedTyping;
    public event Action<FriendRequestDto>? OnFriendRequestReceived;
    public event Action<TaskDto>? OnTaskCreated;
    public event Action<TaskDto>? OnTaskUpdated;
    public event Action<string>? OnTaskDeleted;
    public event Action<ErrorPayload>? OnError;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public WebSocketService(IConfiguration configuration, ILogger<WebSocketService> logger)
    {
        _baseUrl = configuration["ApiSettings:WebSocketUrl"] ?? "ws://localhost:5000/ws";
        _logger = logger;
    }

    /// <summary>
    /// Kết nối WebSocket với JWT token
    /// </summary>
    public async Task ConnectAsync(string jwtToken)
    {
        if (IsConnected)
        {
            _logger.LogWarning("WebSocket already connected");
            return;
        }

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            var uri = new Uri($"{_baseUrl}?token={jwtToken}");

            _logger.LogInformation($"Connecting to WebSocket: {uri}");

            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

            _logger.LogInformation("WebSocket connected successfully");
            OnConnected?.Invoke();

            // Start receiving messages loop
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect WebSocket");
            throw;
        }
    }

    /// <summary>
    /// Disconnect WebSocket
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();

            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnecting",
                CancellationToken.None);

            _logger.LogInformation("WebSocket disconnected");
            OnDisconnected?.Invoke("Normal closure");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting WebSocket");
        }
    }

    /// <summary>
    /// Send message qua WebSocket
    /// </summary>
    public async Task SendAsync(WebSocketMessage message)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send message: WebSocket not connected");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket!.SendAsync(buffer, NetWebSocketMessageType.Text, true, CancellationToken.None);

            _logger.LogDebug($"Sent message type: {message.Type}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message type {message.Type}");
            throw;
        }
    }

    /// <summary>
    /// Join room
    /// </summary>
    public async Task JoinRoomAsync(string roomId)
    {
        var message = new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.JoinRoom,
            Data = JsonSerializer.Serialize(new JoinRoomPayload { RoomId = roomId }),
            Timestamp = DateTime.UtcNow
        };

        await SendAsync(message);
    }

    /// <summary>
    /// Leave room
    /// </summary>
    public async Task LeaveRoomAsync(string roomId)
    {
        var message = new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.LeaveRoom,
            Data = JsonSerializer.Serialize(new JoinRoomPayload { RoomId = roomId }),
            Timestamp = DateTime.UtcNow
        };

        await SendAsync(message);
    }

    /// <summary>
    /// Send chat message
    /// </summary>
    public async Task SendChatMessageAsync(string roomId, string content, MessageType messageType = MessageType.Text)
    {
        var message = new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.ChatMessage,
            Data = JsonSerializer.Serialize(new ChatMessagePayload
            {
                RoomId = roomId,
                Content = content,
                MessageType = messageType
            }),
            Timestamp = DateTime.UtcNow
        };

        await SendAsync(message);
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    public async Task SendTypingAsync(string roomId, bool isTyping)
    {
        var message = new WebSocketMessage
        {
            Type = isTyping ? CustomWebSocketMessageType.UserTyping : CustomWebSocketMessageType.UserStoppedTyping,
            Data = JsonSerializer.Serialize(new TypingPayload { RoomId = roomId }),
            Timestamp = DateTime.UtcNow
        };

        await SendAsync(message);
    }

    /// <summary>
    /// Send ping (heartbeat)
    /// </summary>
    public async Task SendPingAsync()
    {
        var message = new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.Ping,
            Timestamp = DateTime.UtcNow
        };

        await SendAsync(message);
    }

    /// <summary>
    /// Receive messages loop
    /// </summary>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket!.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == NetWebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket close received");
                    OnDisconnected?.Invoke("Server closed connection");
                    break;
                }

                var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(messageChunk);

                if (result.EndOfMessage)
                {
                    var messageJson = messageBuilder.ToString();
                    messageBuilder.Clear();

                    try
                    {
                        await HandleMessageAsync(messageJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error handling message: {messageJson}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop");
            OnDisconnected?.Invoke($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle incoming message
    /// </summary>
    private async Task HandleMessageAsync(string messageJson)
    {
        var message = JsonSerializer.Deserialize<WebSocketMessage>(messageJson);
        if (message == null)
        {
            return;
        }

        _logger.LogDebug($"Received message type: {message.Type}");

        try
        {
            switch (message.Type)
            {
                case CustomWebSocketMessageType.Pong:
                    // Heartbeat response
                    break;

                case CustomWebSocketMessageType.MessageReceived:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var messageDto = JsonSerializer.Deserialize<MessageDto>(message.Data);
                        if (messageDto != null)
                        {
                            OnMessageReceived?.Invoke(messageDto);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserJoinedRoom:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null)
                        {
                            var roomId = data["roomId"].GetString() ?? "";
                            var username = data["username"].GetString() ?? "";
                            OnUserJoinedRoom?.Invoke(roomId, username);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserLeftRoom:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null)
                        {
                            var roomId = data["roomId"].GetString() ?? "";
                            var username = data["username"].GetString() ?? "";
                            OnUserLeftRoom?.Invoke(roomId, username);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserOnline:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null)
                        {
                            var userId = data["userId"].GetString() ?? "";
                            var username = data["username"].GetString() ?? "";
                            OnUserOnline?.Invoke(userId, username);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserOffline:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null)
                        {
                            var userId = data["userId"].GetString() ?? "";
                            OnUserOffline?.Invoke(userId);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserTyping:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var payload = JsonSerializer.Deserialize<TypingPayload>(message.Data);
                        if (payload != null)
                        {
                            OnUserTyping?.Invoke(payload);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.UserStoppedTyping:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var payload = JsonSerializer.Deserialize<TypingPayload>(message.Data);
                        if (payload != null)
                        {
                            OnUserStoppedTyping?.Invoke(payload);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.FriendRequestReceived:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var friendRequest = JsonSerializer.Deserialize<FriendRequestDto>(message.Data);
                        if (friendRequest != null)
                        {
                            OnFriendRequestReceived?.Invoke(friendRequest);
                        }
                    }
                    break;

                case CustomWebSocketMessageType.TaskCreated:
                case CustomWebSocketMessageType.TaskUpdated:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null && data.ContainsKey("task"))
                        {
                            var taskDto = JsonSerializer.Deserialize<TaskDto>(data["task"].GetRawText());
                            if (taskDto != null)
                            {
                                if (message.Type == CustomWebSocketMessageType.TaskCreated)
                                    OnTaskCreated?.Invoke(taskDto);
                                else
                                    OnTaskUpdated?.Invoke(taskDto);
                            }
                        }
                    }
                    break;

                case CustomWebSocketMessageType.TaskDeleted:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.Data);
                        if (data != null && data.ContainsKey("taskId"))
                        {
                            var taskId = data["taskId"].GetString();
                            if (!string.IsNullOrEmpty(taskId))
                            {
                                OnTaskDeleted?.Invoke(taskId);
                            }
                        }
                    }
                    break;

                case CustomWebSocketMessageType.Error:
                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        var error = JsonSerializer.Deserialize<ErrorPayload>(message.Data);
                        if (error != null)
                        {
                            OnError?.Invoke(error);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message type {message.Type}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();
    }
}
