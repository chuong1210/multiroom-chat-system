using ChatRoomSystem.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho Task (Trello-like board)
/// </summary>
public class ChatRoomTask
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TaskStatus Status { get; set; } = TaskStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public string? AssigneeId { get; set; }

    [Required]
    public string CreatorId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    public int BoardPosition { get; set; }

    [MaxLength(1000)]
    public string? TagsJson { get; set; } // JSON serialized array of tags

    // Navigation properties
    public virtual Room Room { get; set; } = null!;

    public virtual ApplicationUser? Assignee { get; set; }

    public virtual ApplicationUser Creator { get; set; } = null!;

    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
}
