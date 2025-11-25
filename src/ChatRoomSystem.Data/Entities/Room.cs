using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Room/Group chat
/// </summary>
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

    // Navigation properties
    public virtual ApplicationUser Creator { get; set; } = null!;

    public virtual ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<ChatRoomTask> Tasks { get; set; } = new List<ChatRoomTask>();

    public virtual ICollection<RoomInvitation> Invitations { get; set; } = new List<RoomInvitation>();
}
