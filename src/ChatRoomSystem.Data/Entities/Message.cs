// ChatRoomSystem.Data/Entities/Message.cs
using ChatRoomSystem.Shared.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

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

    // ✅ File attachments
    [MaxLength(500)]
    public string? FileUrl { get; set; }

    [MaxLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    [MaxLength(100)]
    public string? FileMimeType { get; set; }

    // ✅ Media metadata
    public int? MediaWidth { get; set; }
    public int? MediaHeight { get; set; }
    public int? MediaDuration { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsRead { get; set; }


    // Navigation properties
    public virtual Room Room { get; set; } = null!;

    public virtual ApplicationUser Sender { get; set; } = null!;
}