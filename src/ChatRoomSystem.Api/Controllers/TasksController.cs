using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho Task management (Trello-like board)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        ILogger<TasksController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get tất cả tasks của room
    /// </summary>
    [HttpGet("room/{roomId}")]
    public async Task<ActionResult<List<TaskDto>>> GetRoomTasks(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check if user is member
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        var tasks = await _dbContext.Tasks
            .Where(t => t.RoomId == roomId)
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .OrderBy(t => t.BoardPosition)
            .ToListAsync();

        var taskDtos = tasks.Select(t => MapToDto(t)).ToList();

        return Ok(taskDtos);
    }

    /// <summary>
    /// Tạo task mới
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check if user is member
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == model.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        // Get next board position
        var maxPosition = await _dbContext.Tasks
            .Where(t => t.RoomId == model.RoomId && t.Status == Shared.Models.TaskStatus.ToDo)
            .MaxAsync(t => (int?)t.BoardPosition) ?? 0;

        var task = new ChatRoomTask
        {
            RoomId = model.RoomId,
            Title = model.Title,
            Description = model.Description,
            Priority = model.Priority,
            AssigneeId = model.AssigneeId,
            CreatorId = userId,
            Status = Shared.Models.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow,
            DueDate = model.DueDate,
            BoardPosition = maxPosition + 1,
            TagsJson = model.Tags.Any() ? JsonSerializer.Serialize(model.Tags) : null
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Task {task.Title} created in room {model.RoomId} by user {userId}");

        // Load full task
        var fullTask = await _dbContext.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        var taskDto = MapToDto(fullTask!);

        // Broadcast to room
        await _wsManager.BroadcastToRoomAsync(model.RoomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.TaskCreated,
            Data = JsonSerializer.Serialize(new { roomId = model.RoomId, task = taskDto })
        });

        return Ok(taskDto);
    }

    /// <summary>
    /// Update task
    /// </summary>
    [HttpPut("{taskId}")]
    public async Task<ActionResult<TaskDto>> UpdateTask(string taskId, [FromBody] UpdateTaskDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var task = await _dbContext.Tasks
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return NotFound(new { message = "Task không tồn tại" });
        }

        // Check if user is member
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == task.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        // Update fields
        if (!string.IsNullOrEmpty(model.Title))
        {
            task.Title = model.Title;
        }

        if (model.Description != null)
        {
            task.Description = model.Description;
        }

        if (model.Status.HasValue)
        {
            task.Status = model.Status.Value;
        }

        if (model.Priority.HasValue)
        {
            task.Priority = model.Priority.Value;
        }

        if (model.AssigneeId != null)
        {
            task.AssigneeId = model.AssigneeId;
        }

        if (model.DueDate.HasValue)
        {
            task.DueDate = model.DueDate;
        }

        if (model.BoardPosition.HasValue)
        {
            task.BoardPosition = model.BoardPosition.Value;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Task {taskId} updated by user {userId}");

        // Load full task
        var fullTask = await _dbContext.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        var taskDto = MapToDto(fullTask!);

        // Broadcast to room
        await _wsManager.BroadcastToRoomAsync(task.RoomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.TaskUpdated,
            Data = JsonSerializer.Serialize(new { roomId = task.RoomId, task = taskDto })
        });

        return Ok(taskDto);
    }

    /// <summary>
    /// Delete task
    /// </summary>
    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(string taskId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var task = await _dbContext.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = "Task không tồn tại" });
        }

        // Check if user is creator or room admin
        var membership = await _dbContext.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == task.RoomId && rm.UserId == userId);

        if (membership == null || (task.CreatorId != userId && !membership.IsAdmin))
        {
            return Forbid();
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Task {taskId} deleted by user {userId}");

        // Broadcast to room
        await _wsManager.BroadcastToRoomAsync(task.RoomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.TaskDeleted,
            Data = JsonSerializer.Serialize(new { roomId = task.RoomId, taskId })
        });

        return Ok(new { message = "Task deleted successfully" });
    }

    /// <summary>
    /// Add comment to task
    /// </summary>
    [HttpPost("{taskId}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddComment(string taskId, [FromBody] AddTaskCommentDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var task = await _dbContext.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = "Task không tồn tại" });
        }

        // Check if user is member
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == task.RoomId && rm.UserId == userId);

        if (!isMember)
        {
            return Forbid();
        }

        var user = await _dbContext.Users.FindAsync(userId);
        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = userId,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskComments.Add(comment);
        await _dbContext.SaveChangesAsync();

        var commentDto = new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = taskId,
            UserId = userId,
            Username = user?.UserName ?? "Unknown",
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };

        // Broadcast to room
        await _wsManager.BroadcastToRoomAsync(task.RoomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.TaskCommentAdded,
            Data = JsonSerializer.Serialize(new { roomId = task.RoomId, taskId, comment = commentDto })
        });

        return Ok(commentDto);
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }

    private TaskDto MapToDto(ChatRoomTask task)
    {
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(task.TagsJson))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(task.TagsJson) ?? new List<string>();
            }
            catch { }
        }

        return new TaskDto
        {
            Id = task.Id,
            RoomId = task.RoomId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            AssigneeId = task.AssigneeId,
            AssigneeUsername = task.Assignee?.UserName,
            CreatorId = task.CreatorId,
            CreatorUsername = task.Creator?.UserName ?? "Unknown",
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            BoardPosition = task.BoardPosition,
            Tags = tags,
            Comments = task.Comments?.Select(c => new TaskCommentDto
            {
                Id = c.Id,
                TaskId = c.TaskId,
                UserId = c.UserId,
                Username = c.User?.UserName ?? "Unknown",
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).ToList() ?? new List<TaskCommentDto>()
        };
    }

    #endregion
}
