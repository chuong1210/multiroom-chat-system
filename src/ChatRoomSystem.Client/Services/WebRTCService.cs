using System.Text.Json;
using ChatRoomSystem.Shared.Models;
using Microsoft.JSInterop;
using WebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;

namespace ChatRoomSystem.Client.Services;

/// <summary>
/// Service quản lý WebRTC connections cho video call và screen sharing
/// </summary>
public class WebRTCService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly WebSocketService _webSocketService;
    private readonly ILogger<WebRTCService> _logger;
    private DotNetObjectReference<WebRTCService>? _objRef;

    // Active calls tracking
    private readonly Dictionary<string, CallSession> _activeCalls = new();

    // Events
    public event Action<string, string>? OnIncomingCall;
    public event Action<string>? OnCallAccepted;
    public event Action<string>? OnCallRejected;
    public event Action<string>? OnCallEnded;
    public event Action<string, string>? OnRemoteStreamAvailable;

    public WebRTCService(
        IJSRuntime jsRuntime,
        WebSocketService webSocketService,
        ILogger<WebRTCService> logger)
    {
        _jsRuntime = jsRuntime;
        _webSocketService = webSocketService;
        _logger = logger;

        _objRef = DotNetObjectReference.Create(this);
    }

    /// <summary>
    /// Initialize WebRTC (request camera/mic permissions)
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webRTC.initialize", _objRef);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize WebRTC");
            return false;
        }
    }

    /// <summary>
    /// Initiate video call đến user
    /// </summary>
    public async Task<string> InitiateCallAsync(string toUserId, string roomId, bool includeVideo = true, bool includeAudio = true)
    {
        var callId = Guid.NewGuid().ToString();

        try
        {
            // Create peer connection
            await _jsRuntime.InvokeVoidAsync("webRTC.createPeerConnection", callId);

            // Get local stream
            await _jsRuntime.InvokeVoidAsync("webRTC.getLocalStream", callId, includeVideo, includeAudio);

            // Create offer
            var offerSdp = await _jsRuntime.InvokeAsync<string>("webRTC.createOffer", callId);

            // Track call session
            _activeCalls[callId] = new CallSession
            {
                CallId = callId,
                RemoteUserId = toUserId,
                RoomId = roomId,
                IsInitiator = true,
                StartedAt = DateTime.UtcNow
            };

            // Send offer qua WebSocket
            await _webSocketService.SendAsync(new WebSocketMessage
            {
                Type = WebSocketMessageType.CallInitiate,
                Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                {
                    CallId = callId,
                    FromUserId = "", // Will be set by server
                    ToUserId = toUserId,
                    RoomId = roomId
                })
            });

            await _webSocketService.SendAsync(new WebSocketMessage
            {
                Type = WebSocketMessageType.WebRTCOffer,
                Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                {
                    CallId = callId,
                    FromUserId = "",
                    ToUserId = toUserId,
                    Sdp = offerSdp
                })
            });

            _logger.LogInformation($"Call initiated: {callId} to {toUserId}");
            return callId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call");
            throw;
        }
    }

    /// <summary>
    /// Accept incoming call
    /// </summary>
    public async Task AcceptCallAsync(string callId, string fromUserId)
    {
        try
        {
            // Create peer connection
            await _jsRuntime.InvokeVoidAsync("webRTC.createPeerConnection", callId);

            // Get local stream
            await _jsRuntime.InvokeVoidAsync("webRTC.getLocalStream", callId, true, true);

            // Track call session
            _activeCalls[callId] = new CallSession
            {
                CallId = callId,
                RemoteUserId = fromUserId,
                IsInitiator = false,
                StartedAt = DateTime.UtcNow
            };

            // Send accept message
            await _webSocketService.SendAsync(new WebSocketMessage
            {
                Type = WebSocketMessageType.CallAccept,
                Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                {
                    CallId = callId,
                    ToUserId = fromUserId
                })
            });

            _logger.LogInformation($"Call accepted: {callId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept call");
            throw;
        }
    }

    /// <summary>
    /// Reject incoming call
    /// </summary>
    public async Task RejectCallAsync(string callId, string fromUserId)
    {
        await _webSocketService.SendAsync(new WebSocketMessage
        {
            Type = WebSocketMessageType.CallReject,
            Data = JsonSerializer.Serialize(new WebRTCSignalPayload
            {
                CallId = callId,
                ToUserId = fromUserId
            })
        });

        _logger.LogInformation($"Call rejected: {callId}");
    }

    /// <summary>
    /// End active call
    /// </summary>
    public async Task EndCallAsync(string callId)
    {
        try
        {
            if (_activeCalls.TryGetValue(callId, out var session))
            {
                // Close peer connection
                await _jsRuntime.InvokeVoidAsync("webRTC.closeConnection", callId);

                // Send end message
                await _webSocketService.SendAsync(new WebSocketMessage
                {
                    Type = WebSocketMessageType.CallEnd,
                    Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                    {
                        CallId = callId,
                        ToUserId = session.RemoteUserId
                    })
                });

                _activeCalls.Remove(callId);

                _logger.LogInformation($"Call ended: {callId}");
                OnCallEnded?.Invoke(callId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to end call {callId}");
        }
    }

    /// <summary>
    /// Handle WebRTC offer từ remote peer
    /// </summary>
    public async Task HandleOfferAsync(string callId, string offerSdp)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webRTC.setRemoteDescription", callId, offerSdp, "offer");

            // Create answer
            var answerSdp = await _jsRuntime.InvokeAsync<string>("webRTC.createAnswer", callId);

            if (_activeCalls.TryGetValue(callId, out var session))
            {
                // Send answer
                await _webSocketService.SendAsync(new WebSocketMessage
                {
                    Type = WebSocketMessageType.WebRTCAnswer,
                    Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                    {
                        CallId = callId,
                        ToUserId = session.RemoteUserId,
                        Sdp = answerSdp
                    })
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle offer");
        }
    }

    /// <summary>
    /// Handle WebRTC answer từ remote peer
    /// </summary>
    public async Task HandleAnswerAsync(string callId, string answerSdp)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webRTC.setRemoteDescription", callId, answerSdp, "answer");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle answer");
        }
    }

    /// <summary>
    /// Handle ICE candidate từ remote peer
    /// </summary>
    public async Task HandleIceCandidateAsync(string callId, string candidate)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webRTC.addIceCandidate", callId, candidate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle ICE candidate");
        }
    }

    /// <summary>
    /// Toggle video on/off
    /// </summary>
    public async Task ToggleVideoAsync(string callId)
    {
        await _jsRuntime.InvokeVoidAsync("webRTC.toggleVideo", callId);
    }

    /// <summary>
    /// Toggle audio on/off
    /// </summary>
    public async Task ToggleAudioAsync(string callId)
    {
        await _jsRuntime.InvokeVoidAsync("webRTC.toggleAudio", callId);
    }

    /// <summary>
    /// Start screen sharing
    /// </summary>
    public async Task StartScreenShareAsync(string callId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("webRTC.startScreenShare", callId);

            if (_activeCalls.TryGetValue(callId, out var session))
            {
                // Notify remote peer
                await _webSocketService.SendAsync(new WebSocketMessage
                {
                    Type = WebSocketMessageType.ScreenShareStart,
                    Data = JsonSerializer.Serialize(new { callId, roomId = session.RoomId })
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start screen share");
            throw;
        }
    }

    /// <summary>
    /// Stop screen sharing
    /// </summary>
    public async Task StopScreenShareAsync(string callId)
    {
        await _jsRuntime.InvokeVoidAsync("webRTC.stopScreenShare", callId);

        if (_activeCalls.TryGetValue(callId, out var session))
        {
            await _webSocketService.SendAsync(new WebSocketMessage
            {
                Type = WebSocketMessageType.ScreenShareStop,
                Data = JsonSerializer.Serialize(new { callId, roomId = session.RoomId })
            });
        }
    }

    /// <summary>
    /// Callback từ JS khi có ICE candidate mới
    /// </summary>
    [JSInvokable]
    public async Task OnIceCandidate(string callId, string candidate)
    {
        if (_activeCalls.TryGetValue(callId, out var session))
        {
            await _webSocketService.SendAsync(new WebSocketMessage
            {
                Type = WebSocketMessageType.WebRTCIceCandidate,
                Data = JsonSerializer.Serialize(new WebRTCSignalPayload
                {
                    CallId = callId,
                    ToUserId = session.RemoteUserId,
                    Candidate = candidate
                })
            });
        }
    }

    /// <summary>
    /// Callback từ JS khi remote stream available
    /// </summary>
    [JSInvokable]
    public void OnRemoteStream(string callId, string streamId)
    {
        _logger.LogInformation($"Remote stream available: {callId}");
        OnRemoteStreamAvailable?.Invoke(callId, streamId);
    }

    public async ValueTask DisposeAsync()
    {
        // Close all active calls
        foreach (var callId in _activeCalls.Keys.ToList())
        {
            await EndCallAsync(callId);
        }

        _objRef?.Dispose();
    }
}

/// <summary>
/// Track active call session
/// </summary>
public class CallSession
{
    public string CallId { get; set; } = "";
    public string RemoteUserId { get; set; } = "";
    public string? RoomId { get; set; }
    public bool IsInitiator { get; set; }
    public DateTime StartedAt { get; set; }
}
