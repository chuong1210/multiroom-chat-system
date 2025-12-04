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
/// Controller cho Friend management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<FriendsController> _logger;

    public FriendsController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        ILogger<FriendsController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get danh sách bạn bè
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetFriends()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var friendships = await _dbContext.Friendships
            .Where(f => f.User1Id == userId || f.User2Id == userId)
            .Include(f => f.User1)
            .Include(f => f.User2)
            .ToListAsync();

        var friends = friendships.Select(f =>
        {
            var friend = f.User1Id == userId ? f.User2 : f.User1;
            return new UserDto
            {
                Id = friend.Id,
                Username = friend.UserName!,
                Email = friend.Email!,
                IsOnline = _wsManager.IsUserOnline(friend.Id),
                LastSeen = friend.LastSeen,
                AvatarUrl = friend.AvatarUrl
            };
        }).ToList();

        return Ok(friends);
    }

    /// <summary>
    /// Send friend request
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (userId == model.ToUserId)
        {
            return BadRequest(new { message = "Không thể gửi friend request cho chính mình" });
        }

        // Check if target user exists
        var targetUser = await _dbContext.Users.FindAsync(model.ToUserId);
        if (targetUser == null)
        {
            return NotFound(new { message = "User không tồn tại" });
        }

        // Check if already friends
        var existingFriendship = await _dbContext.Friendships
            .AnyAsync(f => (f.User1Id == userId && f.User2Id == model.ToUserId) ||
                          (f.User1Id == model.ToUserId && f.User2Id == userId));

        if (existingFriendship)
        {
            return BadRequest(new { message = "Đã là bạn bè" });
        }

        // Check if pending request exists
        var pendingRequest = await _dbContext.FriendRequests
            .FirstOrDefaultAsync(fr =>
                ((fr.FromUserId == userId && fr.ToUserId == model.ToUserId) ||
                 (fr.FromUserId == model.ToUserId && fr.ToUserId == userId)) &&
                fr.Status == FriendRequestStatus.Pending);

        if (pendingRequest != null)
        {
            return BadRequest(new { message = "Friend request đã tồn tại" });
        }

        // Create friend request
        var currentUser = await _dbContext.Users.FindAsync(userId);
        var request = new FriendRequest
        {
            FromUserId = userId,
            ToUserId = model.ToUserId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FriendRequests.Add(request);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Friend request sent from {userId} to {model.ToUserId}");

        // Send notification via WebSocket
        await _wsManager.SendMessageAsync(model.ToUserId, new WebSocketMessage
        {
            Type = WebSocketMessageType.FriendRequestReceived,
            Data = JsonSerializer.Serialize(new FriendRequestDto
            {
                Id = request.Id,
                FromUserId = userId,
                FromUsername = currentUser?.UserName ?? "Unknown",
                ToUserId = model.ToUserId,
                ToUsername = targetUser.UserName ?? "Unknown",
                Status = FriendRequestStatus.Pending,
                CreatedAt = request.CreatedAt
            })
        });

        return Ok(new { message = "Friend request sent successfully" });
    }

    /// <summary>
    /// Get pending friend requests
    /// </summary>
    [HttpGet("requests")]
    public async Task<ActionResult<List<FriendRequestDto>>> GetFriendRequests()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var requests = await _dbContext.FriendRequests
            .Where(fr => fr.ToUserId == userId && fr.Status == FriendRequestStatus.Pending)
            .Include(fr => fr.FromUser)
            .Include(fr => fr.ToUser)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync();

        var requestDtos = requests.Select(r => new FriendRequestDto
        {
            Id = r.Id,
            FromUserId = r.FromUserId,
            FromUsername = r.FromUser.UserName ?? "Unknown",
            ToUserId = r.ToUserId,
            ToUsername = r.ToUser.UserName ?? "Unknown",
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            RespondedAt = r.RespondedAt
        }).ToList();

        return Ok(requestDtos);
    }

    /// <summary>
    /// Accept or reject friend request
    /// </summary>
    [HttpPost("requests/{requestId}/respond")]
    public async Task<IActionResult> RespondFriendRequest(string requestId, [FromBody] RespondFriendRequestDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var request = await _dbContext.FriendRequests
            .Include(fr => fr.FromUser)
            .FirstOrDefaultAsync(fr => fr.Id == requestId);

        if (request == null)
        {
            return NotFound(new { message = "Friend request không tồn tại" });
        }

        if (request.ToUserId != userId)
        {
            return Forbid();
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            return BadRequest(new { message = "Request đã được xử lý" });
        }

        // Update request status
        request.Status = model.Accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        // If accepted, create friendship
        if (model.Accept)
        {
            var friendship = new Friendship
            {
                User1Id = request.FromUserId,
                User2Id = request.ToUserId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Friendships.Add(friendship);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Friend request {requestId} {(model.Accept ? "accepted" : "rejected")} by {userId}");

        // Notify sender via WebSocket
        var messageType = model.Accept
            ? WebSocketMessageType.FriendRequestAccepted
            : WebSocketMessageType.FriendRequestRejected;

        await _wsManager.SendMessageAsync(request.FromUserId, new WebSocketMessage
        {
            Type = messageType,
            Data = JsonSerializer.Serialize(new
            {
                requestId,
                fromUserId = request.FromUserId,
                toUserId = request.ToUserId,
                toUsername = request.FromUser.UserName ?? "Unknown"
            })
        });

        return Ok(new { message = model.Accept ? "Friend request accepted" : "Friend request rejected" });
    }

    /// <summary>
    /// Remove friend
    /// </summary>
    [HttpDelete("{friendId}")]
    public async Task<IActionResult> RemoveFriend(string friendId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.User1Id == userId && f.User2Id == friendId) ||
                (f.User1Id == friendId && f.User2Id == userId));

        if (friendship == null)
        {
            return NotFound(new { message = "Friendship không tồn tại" });
        }

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Friendship removed between {userId} and {friendId}");

        return Ok(new { message = "Friend removed successfully" });
    }

    /// <summary>
    /// Search users by username or email
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<UserDto>>> SearchUsers([FromQuery] string query)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        var users = await _dbContext.Users
            .Where(u => u.Id != userId &&
                       (u.UserName!.Contains(query) || u.Email!.Contains(query)))
            .Take(20)
            .ToListAsync();

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.UserName!,
            Email = u.Email!,
            IsOnline = _wsManager.IsUserOnline(u.Id),
            LastSeen = u.LastSeen,
            AvatarUrl = u.AvatarUrl
        }).ToList();

        return Ok(userDtos);
    }

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }
}
