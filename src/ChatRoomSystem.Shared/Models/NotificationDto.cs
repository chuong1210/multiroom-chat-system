using System;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Enum cho notification types
/// </summary>
public enum NotificationType
{
    FriendRequest,
    FriendAccepted,
    FriendRejected,
    NewMessage,
    RoomInvite,
    Mention,
    TaskAssigned,
    TaskUpdated,
    System
}

/// <summary>
/// DTO cho Notification
/// </summary>
public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedUserId { get; set; }
    public string? RelatedRoomId { get; set; }
    public string? RelatedTaskId { get; set; }
}

/// <summary>
/// DTO cho mark notification as read
/// </summary>
public class MarkNotificationReadDto
{
    public string NotificationId { get; set; } = string.Empty;
}