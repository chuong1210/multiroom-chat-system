using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;

namespace ChatRoomSystem.Api.Services;

/// <summary>
/// Service để tạo notifications
/// </summary>
public class NotificationService
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ChatRoomDbContext dbContext,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create friend request notification
    /// </summary>
    public async Task CreateFriendRequestNotificationAsync(string toUserId, string fromUsername)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.FriendRequest,
            Title = "New friend request",
            Message = $"{fromUsername} wants to be your friend",
            ActionText = "View request",
            ActionUrl = "/friends",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create friend accepted notification
    /// </summary>
    public async Task CreateFriendAcceptedNotificationAsync(string toUserId, string acceptedUsername, string acceptedUserId)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.FriendAccepted,
            Title = "Friend request accepted",
            Message = $"{acceptedUsername} accepted your friend request",
            ActionText = "Send message",
            ActionUrl = $"/chat/{acceptedUserId}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedUserId = acceptedUserId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create new message notification
    /// </summary>
    public async Task CreateNewMessageNotificationAsync(string toUserId, string fromUsername, string messagePreview, string roomId)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.NewMessage,
            Title = $"New message from {fromUsername}",
            Message = messagePreview,
            ActionText = "Reply",
            ActionUrl = $"/chat/{toUserId}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRoomId = roomId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create room invite notification
    /// </summary>
    public async Task CreateRoomInviteNotificationAsync(string toUserId, string roomName, string roomId)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.RoomInvite,
            Title = "Room invitation",
            Message = $"You've been invited to join '{roomName}' room",
            ActionText = "View room",
            ActionUrl = $"/room/{roomId}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRoomId = roomId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create mention notification
    /// </summary>
    public async Task CreateMentionNotificationAsync(string toUserId, string mentionedByUsername, string roomName, string roomId)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.Mention,
            Title = "You were mentioned",
            Message = $"{mentionedByUsername} mentioned you in '{roomName}' room",
            ActionText = "View message",
            ActionUrl = $"/room/{roomId}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRoomId = roomId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Create task assigned notification
    /// </summary>
    public async Task CreateTaskAssignedNotificationAsync(string toUserId, string taskTitle, string taskId, string roomId)
    {
        var notification = new Notification
        {
            UserId = toUserId,
            Type = NotificationType.TaskAssigned,
            Title = "Task assigned to you",
            Message = $"You've been assigned to task: {taskTitle}",
            ActionText = "View task",
            ActionUrl = $"/room/{roomId}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedTaskId = taskId,
            RelatedRoomId = roomId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }
}