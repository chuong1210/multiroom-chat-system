using ChatRoomSystem.Data;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho Messages (lấy lịch sử tin nhắn)
/// Gửi tin nhắn real-time qua WebSocket
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(ChatRoomDbContext dbContext, ILogger<MessagesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get messages của room với pagination
    /// </summary>
    [HttpGet("room/{roomId}")]
    public async Task<ActionResult<MessagePageDto>> GetRoomMessages(
        string roomId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
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

        // Get total count
        var totalCount = await _dbContext.Messages
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .CountAsync();

        // Get messages với pagination (newest first)
        var messages = await _dbContext.Messages
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Reverse để oldest first trong page
        messages.Reverse();

        var messageDtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            RoomId = m.RoomId,
            SenderId = m.SenderId,
            SenderUsername = m.Sender.UserName ?? m.Sender.DisplayName,
            Content = m.Content,
            Type = m.Type,
            Timestamp = m.Timestamp,
            FileUrl = m.FileUrl,
            FileName = m.FileName,
            FileSize = m.FileSize
        }).ToList();

        var response = new MessagePageDto
        {
            Messages = messageDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMore = (pageNumber * pageSize) < totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get private messages giữa 2 users
    /// </summary>
    [HttpGet("private/{otherUserId}")]
    public async Task<ActionResult<List<MessageDto>>> GetPrivateMessages(
        string otherUserId,
        [FromQuery] int limit = 50)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find private room giữa 2 users (nếu tồn tại)
        var privateRoom = await _dbContext.Rooms
            .Where(r => r.IsPrivate && r.Members.Count == 2)
            .Where(r => r.Members.Any(m => m.UserId == userId) &&
                       r.Members.Any(m => m.UserId == otherUserId))
            .FirstOrDefaultAsync();

        if (privateRoom == null)
        {
            return Ok(new List<MessageDto>());
        }

        // Get messages
        var messages = await _dbContext.Messages
            .Where(m => m.RoomId == privateRoom.Id && !m.IsDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();

        var messageDtos = messages.Select(m => new MessageDto
        {
            Id = m.Id,
            RoomId = m.RoomId,
            SenderId = m.SenderId,
            SenderUsername = m.Sender.UserName ?? m.Sender.DisplayName,
            Content = m.Content,
            Type = m.Type,
            Timestamp = m.Timestamp,
            FileUrl = m.FileUrl,
            FileName = m.FileName,
            FileSize = m.FileSize
        }).ToList();

        return Ok(messageDtos);
    }

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }
}
