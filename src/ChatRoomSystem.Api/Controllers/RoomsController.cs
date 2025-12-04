using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebSocketMessageType = ChatRoomSystem.Shared.Models.WebSocketMessageType;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho Room management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        ILogger<RoomsController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get tất cả public rooms
    /// </summary>
    [HttpGet("public")]
    public async Task<ActionResult<List<RoomDto>>> GetPublicRooms()
    {
        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsPrivate)
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var roomDtos = rooms.Select(r => MapToDto(r)).ToList();

        return Ok(roomDtos);
    }

    /// <summary>
    /// Get rooms mà user đang tham gia
    /// </summary>
    [HttpGet("my-rooms")]
    public async Task<ActionResult<List<RoomDto>>> GetMyRooms()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var roomIds = await _dbContext.RoomMembers
            .Where(rm => rm.UserId == userId)
            .Select(rm => rm.RoomId)
            .ToListAsync();

        var rooms = await _dbContext.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .OrderByDescending(r => r.LastActivityAt ?? r.CreatedAt)
            .ToListAsync();

        var roomDtos = rooms.Select(r => MapToDto(r)).ToList();

        return Ok(roomDtos);
    }

    /// <summary>
    /// Get room detail by ID
    /// </summary>
    [HttpGet("{roomId}")]
    public async Task<ActionResult<RoomDto>> GetRoom(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await _dbContext.Rooms
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound(new { message = "Room không tồn tại" });
        }

        // Check if user is member (for private rooms)
        if (room.IsPrivate)
        {
            var isMember = room.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                return Forbid();
            }
        }

        return Ok(MapToDto(room));
    }

    /// <summary>
    /// Tạo room mới
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Create room
        var room = new Room
        {
            Name = model.Name,
            Description = model.Description,
            Topic = model.Topic,
            CreatorId = userId,
            IsPrivate = model.IsPrivate,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _dbContext.Rooms.Add(room);

        // Add creator as member và admin
        var membership = new RoomMember
        {
            UserId = userId,
            RoomId = room.Id,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.RoomMembers.Add(membership);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Room {room.Name} created by user {userId}");

        // Load full room data
        var fullRoom = await _dbContext.Rooms
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == room.Id);

        return Ok(MapToDto(fullRoom!));
    }

    /// <summary>
    /// Update room info
    /// </summary>
    [HttpPut("{roomId}")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(string roomId, [FromBody] UpdateRoomDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound(new { message = "Room không tồn tại" });
        }

        // Check if user is admin
        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
        {
            return Forbid();
        }

        // Update fields
        if (!string.IsNullOrEmpty(model.Name))
        {
            room.Name = model.Name;
        }

        if (model.Description != null)
        {
            room.Description = model.Description;
        }

        if (model.Topic != null)
        {
            room.Topic = model.Topic;
        }

        if (model.IsPrivate.HasValue)
        {
            room.IsPrivate = model.IsPrivate.Value;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Room {roomId} updated by user {userId}");

        // Reload with includes
        var fullRoom = await _dbContext.Rooms
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        return Ok(MapToDto(fullRoom!));
    }

    /// <summary>
    /// Join public room
    /// </summary>
    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound(new { message = "Room không tồn tại" });
        }

        // Only allow joining public rooms directly
        if (room.IsPrivate)
        {
            return BadRequest(new { message = "Không thể join private room trực tiếp. Cần invitation." });
        }

        // Check if already member
        var existingMembership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (existingMembership != null)
        {
            return BadRequest(new { message = "Bạn đã là member của room này" });
        }

        // Add membership
        var membership = new RoomMember
        {
            UserId = userId,
            RoomId = roomId,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.RoomMembers.Add(membership);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"User {userId} joined room {roomId}");

        // Notify online users in room via WebSocket
        var user = await _dbContext.Users.FindAsync(userId);
        await _wsManager.BroadcastToRoomAsync(roomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.UserJoinedRoom,
            Data = JsonSerializer.Serialize(new
            {
                roomId,
                userId,
                username = user?.UserName ?? "Unknown"
            })
        });

        return Ok(new { message = "Joined room successfully" });
    }

    /// <summary>
    /// Leave room
    /// </summary>
    [HttpPost("{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var membership = await _dbContext.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (membership == null)
        {
            return NotFound(new { message = "Bạn không phải member của room này" });
        }

        // Remove membership
        _dbContext.RoomMembers.Remove(membership);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"User {userId} left room {roomId}");

        // Notify users
        var user = await _dbContext.Users.FindAsync(userId);
        await _wsManager.BroadcastToRoomAsync(roomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.UserLeftRoom,
            Data = JsonSerializer.Serialize(new
            {
                roomId,
                userId,
                username = user?.UserName ?? "Unknown"
            })
        });

        return Ok(new { message = "Left room successfully" });
    }

    /// <summary>
    /// Search rooms by name or topic
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<RoomDto>>> SearchRooms([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsPrivate &&
                       (r.Name.Contains(query) || r.Topic.Contains(query) || r.Description.Contains(query)))
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync();

        var roomDtos = rooms.Select(r => MapToDto(r)).ToList();

        return Ok(roomDtos);
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }

    private RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            Topic = room.Topic,
            CreatorId = room.CreatorId,
            IsPrivate = room.IsPrivate,
            CreatedAt = room.CreatedAt,
            MemberIds = room.Members.Select(m => m.UserId).ToList(),
            ActiveMembersCount = room.Members.Count(m => _wsManager.IsUserOnline(m.UserId))
        };
    }

    #endregion
}
