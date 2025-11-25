using ChatRoomSystem.Shared.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Friend Request
/// </summary>
public class FriendRequest
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string FromUserId { get; set; } = string.Empty;

    [Required]
    public string ToUserId { get; set; } = string.Empty;

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser FromUser { get; set; } = null!;

    public virtual ApplicationUser ToUser { get; set; } = null!;
}
