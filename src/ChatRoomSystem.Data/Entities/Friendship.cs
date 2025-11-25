using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Friendship (many-to-many relationship giữa Users)
/// </summary>
public class Friendship
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string User1Id { get; set; } = string.Empty;

    [Required]
    public string User2Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser User1 { get; set; } = null!;

    public virtual ApplicationUser User2 { get; set; } = null!;
}
