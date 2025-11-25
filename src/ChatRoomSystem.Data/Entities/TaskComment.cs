using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Task Comment
/// </summary>
public class TaskComment
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string TaskId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ChatRoomTask Task { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
}
