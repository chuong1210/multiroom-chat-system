using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Room membership (many-to-many relationship giữa User và Room)
/// </summary>
public class RoomMember
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string RoomId { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsAdmin { get; set; }

    public bool IsMuted { get; set; }

    public DateTime? LastReadAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
