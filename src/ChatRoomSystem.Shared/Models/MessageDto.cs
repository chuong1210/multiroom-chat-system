using System;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Enum định nghĩa các loại message
/// </summary>
public enum MessageType
{
    Text,           // Tin nhắn text thông thường
    Image,          // Hình ảnh
    File,           // File đính kèm
    Video,          // Video call recording
    System,         // System notification (user joined/left)
    ScreenShare,    // Screen sharing event
    WhiteboardData  // Whiteboard drawing data
}

/// <summary>
/// Data Transfer Object cho Message
/// </summary>
public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime Timestamp { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
}

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

/// <summary>
/// DTO cho pagination messages
/// </summary>
public class MessagePageDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
