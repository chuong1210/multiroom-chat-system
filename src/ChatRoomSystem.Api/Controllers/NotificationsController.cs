using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho Notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ChatRoomDbContext dbContext,
        ILogger<NotificationsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100) // Limit to last 100 notifications
            .ToListAsync();

        var notificationDtos = notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            ActionText = n.ActionText,
            ActionUrl = n.ActionUrl,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            RelatedUserId = n.RelatedUserId,
            RelatedRoomId = n.RelatedRoomId,
            RelatedTaskId = n.RelatedTaskId
        }).ToList();

        return Ok(notificationDtos);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        return Ok(count);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(string notificationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return NotFound(new { message = "Notification không tồn tại" });
        }

        notification.IsRead = true;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Notification marked as read" });
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"User {userId} marked {notifications.Count} notifications as read");

        return Ok(new { message = $"{notifications.Count} notifications marked as read" });
    }

    /// <summary>
    /// Dismiss/Delete notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DismissNotification(string notificationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return NotFound(new { message = "Notification không tồn tại" });
        }

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Notification dismissed" });
    }

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }
}