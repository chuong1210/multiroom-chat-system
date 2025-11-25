using System;
using System.Collections.Generic;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Enum cho các loại WebSocket message
/// </summary>
public enum WebSocketMessageType
{
    // Authentication
    Authenticate,
    AuthenticationSuccess,
    AuthenticationFailed,

    // Chat messages
    ChatMessage,
    MessageReceived,
    MessageHistory,

    // Room management
    JoinRoom,
    LeaveRoom,
    RoomJoined,
    RoomLeft,
    UserJoinedRoom,
    UserLeftRoom,

    // Friend requests
    FriendRequest,
    FriendRequestReceived,
    FriendRequestAccepted,
    FriendRequestRejected,

    // Room invitations
    RoomInvite,
    RoomInviteReceived,
    RoomInviteAccepted,
    RoomInviteRejected,

    // Task updates
    TaskCreated,
    TaskUpdated,
    TaskDeleted,
    TaskCommentAdded,

    // Video call
    CallInitiate,
    CallAccept,
    CallReject,
    CallEnd,
    WebRTCOffer,
    WebRTCAnswer,
    WebRTCIceCandidate,

    // Screen sharing
    ScreenShareStart,
    ScreenShareStop,
    ScreenShareData,

    // Whiteboard
    WhiteboardDraw,
    WhiteboardClear,
    WhiteboardData,

    // User status
    UserOnline,
    UserOffline,
    UserTyping,
    UserStoppedTyping,

    // Heartbeat
    Ping,
    Pong,

    // Error
    Error
}

/// <summary>
/// Base WebSocket message structure
/// </summary>
public class WebSocketMessage
{
    public WebSocketMessageType Type { get; set; }
    public string? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// WebSocket message cho authentication
/// </summary>
public class AuthenticateMessage
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// WebSocket message cho chat
/// </summary>
public class ChatMessagePayload
{
    public string RoomId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
}

/// <summary>
/// WebSocket message cho join room
/// </summary>
public class JoinRoomPayload
{
    public string RoomId { get; set; } = string.Empty;
}

/// <summary>
/// WebSocket message cho typing indicator
/// </summary>
public class TypingPayload
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// WebSocket message cho video call WebRTC signaling
/// </summary>
public class WebRTCSignalPayload
{
    public string CallId { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public string? Sdp { get; set; }
    public string? Candidate { get; set; }
    public string? RoomId { get; set; }
}

/// <summary>
/// WebSocket message cho whiteboard
/// </summary>
public class WhiteboardPayload
{
    public string RoomId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // draw, clear, erase
    public WhiteboardStroke? Stroke { get; set; }
}

/// <summary>
/// Whiteboard stroke data
/// </summary>
public class WhiteboardStroke
{
    public List<WhiteboardPoint> Points { get; set; } = new();
    public string Color { get; set; } = "#000000";
    public int Width { get; set; } = 2;
    public string Tool { get; set; } = "pen"; // pen, eraser, line, rect, circle
}

/// <summary>
/// Whiteboard point
/// </summary>
public class WhiteboardPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Error message payload
/// </summary>
public class ErrorPayload
{
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}
