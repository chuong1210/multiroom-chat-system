using System;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// DTO cho gửi message mới
/// </summary>
public class SendMessageDto
{
    public string RoomId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
}



public enum MessageType
{
    Text,
    Image,
    Video,
    Audio,
    File,
    System,
    ScreenShare,    // Screen sharing event
    WhiteboardData  // Whiteboard drawing data
}

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }

    // ✅ File metadata
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? FileMimeType { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? MediaWidth { get; set; }
    public int? MediaHeight { get; set; }
    public int? MediaDuration { get; set; }
    public bool IsRead { get; set; } = false;

}

public class MessagePageDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
/// <summary>
/// DTO cho Conversation summary (danh sách chat)
/// </summary>
public class ConversationDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsTyping { get; set; } // ✅ Add this
    public bool HasUnread { get; set; }
    public string? RoomId { get; set; }
}