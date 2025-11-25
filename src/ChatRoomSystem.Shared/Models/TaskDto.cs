using System;
using System.Collections.Generic;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Enum cho trạng thái task (Kanban board)
/// </summary>
public enum TaskStatus
{
    ToDo,        // Chưa làm
    InProgress,  // Đang làm
    Done         // Hoàn thành
}

/// <summary>
/// Enum cho mức độ ưu tiên
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// Data Transfer Object cho Task
/// </summary>
public class TaskDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? AssigneeId { get; set; }
    public string? AssigneeUsername { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string CreatorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int BoardPosition { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<TaskCommentDto> Comments { get; set; } = new();
}

/// <summary>
/// DTO cho tạo task mới
/// </summary>
public class CreateTaskDto
{
    public string RoomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// DTO cho update task
/// </summary>
public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public int? BoardPosition { get; set; }
}

/// <summary>
/// DTO cho comment trên task
/// </summary>
public class TaskCommentDto
{
    public string Id { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO cho add comment
/// </summary>
public class AddTaskCommentDto
{
    public string TaskId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
