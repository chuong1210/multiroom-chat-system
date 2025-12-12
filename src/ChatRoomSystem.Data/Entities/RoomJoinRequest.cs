// ChatRoomSystem.Data/Entities/RoomJoinRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

public enum JoinRequestStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// Entity for room join requests (when RequireApproval = true)
/// </summary>
public class RoomJoinRequest
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    public string? RespondedByUserId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; } // Optional message from user

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationUser? RespondedBy { get; set; }
}