// ChatRoomSystem.Data/Entities/Room.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

public class Room
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string CreatorId { get; set; } = string.Empty;

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActivityAt { get; set; }

    // ✅ Room customization
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(500)]
    public string? BackgroundImageUrl { get; set; }

    // ✅ Invite link settings
    [MaxLength(50)]
    public string? InviteCode { get; set; }

    public bool AllowInviteLink { get; set; } = true;

    public bool RequireApproval { get; set; } = false;

    // ✅ Room settings
    public bool AllowMemberInvite { get; set; } = true; // Members can invite others

    public int MaxMembers { get; set; } = 200; // Max room size

    // Navigation properties
    public virtual ApplicationUser Creator { get; set; } = null!;
    public virtual ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<ChatRoomTask> Tasks { get; set; } = new List<ChatRoomTask>();
    public virtual ICollection<RoomInvitation> Invitations { get; set; } = new List<RoomInvitation>();
}