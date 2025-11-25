using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Enum cho trạng thái room invitation
/// </summary>
public enum RoomInvitationStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

/// <summary>
/// Entity cho Room Invitation
/// </summary>
public class RoomInvitation
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    public string InvitedUserId { get; set; } = string.Empty;

    [Required]
    public string InvitedByUserId { get; set; } = string.Empty;

    public RoomInvitationStatus Status { get; set; } = RoomInvitationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public virtual Room Room { get; set; } = null!;

    public virtual ApplicationUser InvitedUser { get; set; } = null!;

    public virtual ApplicationUser InvitedByUser { get; set; } = null!;
}
