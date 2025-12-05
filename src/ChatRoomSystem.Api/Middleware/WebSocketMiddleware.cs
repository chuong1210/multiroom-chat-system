using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Shared.Models;
using Microsoft.IdentityModel.Tokens;
using CustomWebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;

namespace ChatRoomSystem.Api.Middleware;

/// <summary>
/// Middleware xử lý WebSocket connections và messages
/// </summary>
public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        WebSocketConnectionManager connectionManager,
        WebSocketMessageHandler messageHandler,
        IConfiguration configuration)
    {
        // Check if request is WebSocket request
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        // Accept WebSocket connection
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("WebSocket connection established");

        // Authenticate user từ query string token
        var token = context.Request.Query["token"].ToString();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("WebSocket connection attempted without token");
            await socket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Authentication token required",
                CancellationToken.None);
            return;
        }

        // Validate JWT token
        var userId = await ValidateTokenAndGetUserId(token, configuration);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("WebSocket connection attempted with invalid token");
            await socket.CloseAsync(
                WebSocketCloseStatus.PolicyViolation,
                "Invalid authentication token",
                CancellationToken.None);
            return;
        }

        // Get username từ token claims
        var username = GetUsernameFromToken(token);

        // Add connection vào manager
        connectionManager.AddConnection(userId, socket, username);

        // Send authentication success message
        await connectionManager.SendMessageAsync(userId, new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.AuthenticationSuccess,
            Data = JsonSerializer.Serialize(new { userId, username })
        });

        // Broadcast user online status
        await connectionManager.BroadcastToAllAsync(new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.UserOnline,
            Data = JsonSerializer.Serialize(new { userId, username })
        }, excludeUserId: userId);

        try
        {
            // Handle messages loop
            await ReceiveMessagesAsync(socket, userId, connectionManager, messageHandler);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in WebSocket connection for user {userId}");
        }
        finally
        {
            // Cleanup connection
            await CleanupConnection(userId, username, socket, connectionManager);
        }
    }

    /// <summary>
    /// Receive và handle messages từ client
    /// </summary>
    private async Task ReceiveMessagesAsync(
        WebSocket socket,
        string userId,
        WebSocketConnectionManager connectionManager,
        WebSocketMessageHandler messageHandler)
    {
        var buffer = new byte[1024 * 4];
        var messageBuilder = new StringBuilder();

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, $"WebSocket error for user {userId}");
                break;
            }

            if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
            {
                _logger.LogInformation($"WebSocket close request from user {userId}");
                break;
            }

            // Append received data to message builder
            var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
            messageBuilder.Append(messageChunk);

            // If message is complete (EndOfMessage = true), process it
            if (result.EndOfMessage)
            {
                var messageJson = messageBuilder.ToString();
                messageBuilder.Clear();

                try
                {
                    var message = JsonSerializer.Deserialize<WebSocketMessage>(messageJson);
                    if (message != null)
                    {
                        // Handle message
                        await messageHandler.HandleMessageAsync(userId, message, connectionManager);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Invalid JSON message from user {userId}: {messageJson}");
                    await connectionManager.SendMessageAsync(userId, new WebSocketMessage
                    {
                        Type = CustomWebSocketMessageType.Error,
                        Data = JsonSerializer.Serialize(new ErrorPayload
                        {
                            Message = "Invalid message format",
                            Code = "INVALID_JSON"
                        })
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error handling message from user {userId}");
                    await connectionManager.SendMessageAsync(userId, new WebSocketMessage
                    {
                        Type = CustomWebSocketMessageType.Error,
                        Data = JsonSerializer.Serialize(new ErrorPayload
                        {
                            Message = "Error processing message",
                            Code = "PROCESSING_ERROR"
                        })
                    });
                }
            }
        }
    }

    /// <summary>
    /// Cleanup connection khi disconnect
    /// </summary>
    private async Task CleanupConnection(
        string userId,
        string username,
        WebSocket socket,
        WebSocketConnectionManager connectionManager)
    {
        _logger.LogInformation($"Cleaning up connection for user {userId}");

        // Remove connection
        await connectionManager.RemoveConnectionAsync(userId);

        // Broadcast user offline status
        await connectionManager.BroadcastToAllAsync(new WebSocketMessage
        {
            Type = CustomWebSocketMessageType.UserOffline,
            Data = JsonSerializer.Serialize(new { userId, username })
        });

        // Close socket if still open
        if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
        {
            try
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error closing socket for user {userId}");
            }
        }
    }

    /// <summary>
    /// Validate JWT token và get userId
    /// </summary>
    private async Task<string?> ValidateTokenAndGetUserId(string token, IConfiguration configuration)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyHereShouldBeAtLeast32CharactersLong!");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["JwtSettings:Issuer"] ?? "ChatRoomSystem",
                ValidateAudience = true,
                ValidAudience = configuration["JwtSettings:Audience"] ?? "ChatRoomSystem",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userId = principal.FindFirst("userId")?.Value
                      ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Get username từ token
    /// </summary>
    private string GetUsernameFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value
                ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
