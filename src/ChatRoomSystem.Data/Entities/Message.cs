using ChatRoomSystem.Shared.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Message
/// </summary>
public class Message
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public MessageType Type { get; set; } = MessageType.Text;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    [MaxLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public bool IsEdited { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation properties
    public virtual Room Room { get; set; } = null!;

    public virtual ApplicationUser Sender { get; set; } = null!;
}
