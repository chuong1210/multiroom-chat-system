using System;
using System.ComponentModel.DataAnnotations;
using ChatRoomSystem.Shared.Models;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Notification
/// </summary>
public class Notification
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ActionText { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Related entities (optional)
    public string? RelatedUserId { get; set; }
    public string? RelatedRoomId { get; set; }
    public string? RelatedTaskId { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
}