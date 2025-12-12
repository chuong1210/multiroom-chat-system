// ChatRoomSystem.Api/Controllers/RoomsController.cs
using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
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
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        IFileUploadService fileUploadService,
        ILogger<RoomsController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }
    // ChatRoomSystem.Api/Controllers/RoomsController.cs

    [HttpGet("public")]
    public async Task<ActionResult<List<RoomDto>>> GetPublicRooms()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // ✅ FIX: Only return public rooms that user can actually access
        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsPrivate) // Only public rooms
            .Include(r => r.Members)
                            .ThenInclude(m => m.User) // ✅ Include User

            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Topic = r.Topic,
                IsPrivate = r.IsPrivate,
                CreatorId = r.CreatorId,
                CreatedAt = r.CreatedAt,
                MemberIds = r.Members.Select(m => m.UserId).ToList(),
                ActiveMembersCount = r.Members.Count(m => m.User.IsOnline), // ✅ Use User.IsOnline
                AvatarUrl = r.AvatarUrl,
                CoverImageUrl = r.CoverImageUrl,
                BackgroundImageUrl = r.BackgroundImageUrl,
                RequireApproval = r.RequireApproval,
                AllowInviteLink = r.AllowInviteLink,
                AllowMemberInvite = r.AllowMemberInvite,
                MaxMembers = r.MaxMembers,
                CurrentMembersCount = r.Members.Count,
                // ✅ Check if current user is member
                IsAdmin = r.Members.Any(m => m.UserId == userId && m.IsAdmin),
                IsCreator = r.CreatorId == userId
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("{roomId}")]
    public async Task<ActionResult<RoomDto>> GetRoom(string roomId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            return NotFound();
        }

        // ✅ FIX: Check if user is member BEFORE returning room details
        var isMember = room.Members.Any(m => m.UserId == userId);

        // If private room and not a member, return Forbidden
        if (room.IsPrivate && !isMember)
        {
            return Forbid();
        }

        var roomDto = MapToDto(room, userId);
        return Ok(roomDto);
    }

    [HttpGet("{roomId}/members")]
    public async Task<ActionResult<List<RoomMemberDto>>> GetRoomMembers(string roomId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // ✅ FIX: Verify user is member before returning members list
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!isMember)
        {
            return Forbid("You are not a member of this room");
        }

        var members = await _dbContext.RoomMembers
            .Where(rm => rm.RoomId == roomId)
            .Include(rm => rm.User)

            .OrderByDescending(rm => rm.User.UserName == _dbContext.Rooms
                .Where(r => r.Id == roomId)
                .Select(r => r.CreatorId)
                .FirstOrDefault())
            .ThenByDescending(rm => rm.IsAdmin)
        .ThenByDescending(rm => rm.User.IsOnline) // ✅ Use User.IsOnline
            .ThenBy(rm => rm.User.UserName)
            .Select(rm => new RoomMemberDto
            {
                UserId = rm.UserId,
                Username = rm.User.UserName ?? rm.User.DisplayName,
                AvatarUrl = rm.User.AvatarUrl,
                IsAdmin = rm.IsAdmin,
                IsCreator = rm.UserId == _dbContext.Rooms
                    .Where(r => r.Id == roomId)
                    .Select(r => r.CreatorId)
                    .FirstOrDefault(),
                IsOnline = rm.User.IsOnline,
                JoinedAt = rm.JoinedAt,
                LastSeen = rm.User.LastSeen
            })
            .ToListAsync();

        return Ok(members);
    }
    /// <summary>
    /// Get rooms mà user đang tham gia
    /// </summary>
    [HttpGet("my-rooms")]
    public async Task<ActionResult<List<RoomDto>>> GetMyRooms()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var rooms = await _dbContext.RoomMembers
            .Where(rm => rm.UserId == userId)
            .Include(rm => rm.Room)
                .ThenInclude(r => r.Members)
                    .ThenInclude(m => m.User) // ✅ Include User
            .Select(rm => rm.Room)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Topic = r.Topic,
                IsPrivate = r.IsPrivate,
                CreatorId = r.CreatorId,
                CreatedAt = r.CreatedAt,
                MemberIds = r.Members.Select(m => m.UserId).ToList(),
                ActiveMembersCount = r.Members.Count(m => m.User.IsOnline), // ✅ Use User.IsOnline
                AvatarUrl = r.AvatarUrl,
                CoverImageUrl = r.CoverImageUrl,
                BackgroundImageUrl = r.BackgroundImageUrl,
                InviteCode = r.Members.Any(m => m.UserId == userId && m.IsAdmin) ? r.InviteCode : null,
                RequireApproval = r.RequireApproval,
                AllowInviteLink = r.AllowInviteLink,
                AllowMemberInvite = r.AllowMemberInvite,
                MaxMembers = r.MaxMembers,
                CurrentMembersCount = r.Members.Count,
                IsAdmin = r.Members.Any(m => m.UserId == userId && m.IsAdmin),
                IsCreator = r.CreatorId == userId
            })
            .ToListAsync();

        return Ok(rooms);
    }

    ///// <summary>
    ///// Get room detail by ID
    ///// </summary>
    //[HttpGet("{roomId}")]
    //public async Task<ActionResult<RoomDto>> GetRoom(string roomId)
    //{
    //    var userId = GetUserId();
    //    if (string.IsNullOrEmpty(userId))
    //    {
    //        return Unauthorized();
    //    }

    //    var room = await _dbContext.Rooms
    //        .Include(r => r.Creator)
    //        .Include(r => r.Members)
    //        .FirstOrDefaultAsync(r => r.Id == roomId);

    //    if (room == null)
    //    {
    //        return NotFound(new { message = "Room không tồn tại" });
    //    }

    //    // Check if user is member (for private rooms)
    //    if (room.IsPrivate)
    //    {
    //        var isMember = room.Members.Any(m => m.UserId == userId);
    //        if (!isMember)
    //        {
    //            return Forbid();
    //        }
    //    }

    //    return Ok(MapToDto(room, userId));
    //}

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
            RequireApproval = model.RequireApproval,
            AllowMemberInvite = model.AllowMemberInvite,
            MaxMembers = model.MaxMembers > 0 ? model.MaxMembers : 200, // ✅ FIX: Default to 200 if 0

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

        return Ok(MapToDto(fullRoom!, userId));
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

        if (model.RequireApproval.HasValue)
        {
            room.RequireApproval = model.RequireApproval.Value;
        }

        if (model.AllowMemberInvite.HasValue)
        {
            room.AllowMemberInvite = model.AllowMemberInvite.Value;
        }

        if (model.AllowInviteLink.HasValue)
        {
            room.AllowInviteLink = model.AllowInviteLink.Value;
        }

        if (model.MaxMembers.HasValue)
        {
            room.MaxMembers = model.MaxMembers.Value;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Room {roomId} updated by user {userId}");

        // Reload with includes
        var fullRoom = await _dbContext.Rooms
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        return Ok(MapToDto(fullRoom!, userId));
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
            _logger.LogWarning($"🔴 Unauthorized join attempt for room {roomId}");
            return Unauthorized();
        }

        _logger.LogInformation($"🔵 User {userId} attempting to join room {roomId}");

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
        {
            _logger.LogWarning($"🔴 Room {roomId} not found");
            return NotFound(new { message = "Room không tồn tại" });
        }

        _logger.LogInformation($"🔵 Room found: {room.Name}, IsPrivate: {room.IsPrivate}, Members: {room.Members.Count}/{room.MaxMembers}");

        // ✅ FIX: Allow joining public rooms
        if (room.IsPrivate)
        {
            _logger.LogWarning($"🔴 User {userId} tried to join private room {roomId}");
            return BadRequest(new { message = "Không thể join private room trực tiếp. Cần invitation." });
        }

        // Check if already member
        var existingMembership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (existingMembership != null)
        {
            _logger.LogWarning($"⚠️ User {userId} is already a member of room {roomId}");
            return Ok(new { message = "Bạn đã là member của room này", alreadyMember = true });
        }

        // Check if full
        if (room.Members.Count >= room.MaxMembers)
        {
            _logger.LogWarning($"🔴 Room {roomId} is full ({room.Members.Count}/{room.MaxMembers})");
            return BadRequest(new { message = "Room đã đầy" });
        }

        // ✅ If requires approval, create join request instead
        if (room.RequireApproval)
        {
            _logger.LogInformation($"🔵 Room requires approval. Creating join request for user {userId}");

            var existingRequest = await _dbContext.RoomJoinRequests
                .FirstOrDefaultAsync(jr => jr.RoomId == roomId && jr.UserId == userId && jr.Status == JoinRequestStatus.Pending);

            if (existingRequest != null)
            {
                _logger.LogWarning($"⚠️ User {userId} already has pending request for room {roomId}");
                return Ok(new { message = "Bạn đã gửi yêu cầu join. Đang chờ admin phê duyệt.", requiresApproval = true });
            }

            var joinRequest = new RoomJoinRequest
            {
                RoomId = roomId,
                UserId = userId,
                Status = JoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RoomJoinRequests.Add(joinRequest);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"✅ Join request created for user {userId} in room {roomId}");

            return Ok(new { message = "Yêu cầu join đã được gửi. Đang chờ admin phê duyệt.", requiresApproval = true });
        }

        // ✅ Add membership directly for public rooms without approval
        var membership = new RoomMember
        {
            UserId = userId,
            RoomId = roomId,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.RoomMembers.Add(membership);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"✅ User {userId} joined room {roomId} successfully");

        // Notify online users in room via WebSocket
        var user = await _dbContext.Users.FindAsync(userId);
        try
        {
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔴 Failed to broadcast join message for user {userId} in room {roomId}");
        }

        return Ok(new { message = "Joined room successfully", success = true });
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

        var userId = GetUserId();
        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsPrivate &&
                       (r.Name.Contains(query) || r.Topic.Contains(query) || r.Description.Contains(query)))
            .Include(r => r.Creator)
            .Include(r => r.Members)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync();

        var roomDtos = rooms.Select(r => MapToDto(r, userId)).ToList();

        return Ok(roomDtos);
    }

    /// <summary>
    /// Get or create private room giữa 2 users
    /// </summary>
    [HttpPost("private")]
    public async Task<ActionResult> GetOrCreatePrivateRoom([FromBody] CreatePrivateRoomDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check if users are friends
        var areFriends = await _dbContext.Friendships
            .AnyAsync(f => (f.User1Id == userId && f.User2Id == model.OtherUserId) ||
                          (f.User1Id == model.OtherUserId && f.User2Id == userId));

        if (!areFriends)
        {
            return BadRequest(new { message = "Bạn phải là bạn bè để chat riêng" });
        }

        // Check if private room already exists
        var existingRoom = await _dbContext.Rooms
            .Where(r => r.IsPrivate && r.Members.Count == 2)
            .Where(r => r.Members.Any(m => m.UserId == userId) &&
                       r.Members.Any(m => m.UserId == model.OtherUserId))
            .FirstOrDefaultAsync();

        if (existingRoom != null)
        {
            return Ok(new { roomId = existingRoom.Id });
        }

        // Create new private room
        var room = new Room
        {
            Name = $"Private_{userId}_{model.OtherUserId}",
            Description = "Private conversation",
            Topic = "",

            CreatorId = userId,
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        _dbContext.Rooms.Add(room);

        // Add both users as members
        var membership1 = new RoomMember
        {
            UserId = userId,
            RoomId = room.Id,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };

        var membership2 = new RoomMember
        {
            UserId = model.OtherUserId,
            RoomId = room.Id,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.RoomMembers.Add(membership1);
        _dbContext.RoomMembers.Add(membership2);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation($"Private room created between {userId} and {model.OtherUserId}");

        return Ok(new { roomId = room.Id });
    }

    /// <summary>
    /// Generate unique invite code for room
    /// </summary>
    [HttpPost("{roomId}/generate-invite-code")]
    public async Task<ActionResult<string>> GenerateInviteCode(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound(new { message = "Room not found" });

        // Check if user is admin
        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        if (!room.AllowInviteLink)
            return BadRequest(new { message = "Invite links are disabled for this room" });

        // Generate unique code
        room.InviteCode = GenerateUniqueCode();
        await _dbContext.SaveChangesAsync();

        var inviteUrl = $"{Request.Scheme}://{Request.Host}/join/{room.InviteCode}";

        return Ok(new { inviteCode = room.InviteCode, inviteUrl });
    }

    /// <summary>
    /// Get room preview by invite code
    /// </summary>
    [HttpGet("preview/{inviteCode}")]
    public async Task<ActionResult<RoomPreviewDto>> GetRoomPreview(string inviteCode)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.InviteCode == inviteCode);

        if (room == null)
            return NotFound(new { message = "Invalid invite code" });

        var isMember = room.Members.Any(m => m.UserId == userId);
        var pendingRequest = await _dbContext.RoomJoinRequests
            .AnyAsync(jr => jr.RoomId == room.Id && jr.UserId == userId && jr.Status == JoinRequestStatus.Pending);

        return Ok(new RoomPreviewDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            AvatarUrl = room.AvatarUrl,
            CoverImageUrl = room.CoverImageUrl,
            MembersCount = room.Members.Count,
            RequireApproval = room.RequireApproval,
            IsFull = room.Members.Count >= room.MaxMembers,
            AlreadyMember = isMember,
            HasPendingRequest = pendingRequest
        });
    }

    /// <summary>
    /// Join room via invite code
    /// </summary>
    [HttpPost("join-by-code/{inviteCode}")]
    public async Task<IActionResult> JoinRoomByCode(string inviteCode, [FromBody] JoinRoomRequestDto? model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.InviteCode == inviteCode);

        if (room == null)
            return NotFound(new { message = "Invalid invite code" });

        // Check if already member
        if (room.Members.Any(m => m.UserId == userId))
            return BadRequest(new { message = "Already a member" });

        // Check if full
        if (room.Members.Count >= room.MaxMembers)
            return BadRequest(new { message = "Room is full" });

        // If requires approval, create join request
        if (room.RequireApproval)
        {
            var existingRequest = await _dbContext.RoomJoinRequests
                .FirstOrDefaultAsync(jr => jr.RoomId == room.Id && jr.UserId == userId && jr.Status == JoinRequestStatus.Pending);

            if (existingRequest != null)
                return BadRequest(new { message = "You already have a pending request" });

            var joinRequest = new RoomJoinRequest
            {
                RoomId = room.Id,
                UserId = userId,
                Message = model?.Message,
                Status = JoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RoomJoinRequests.Add(joinRequest);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Join request sent. Waiting for approval.", requiresApproval = true });
        }

        // Join directly
        var membership = new RoomMember
        {
            UserId = userId,
            RoomId = room.Id,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.RoomMembers.Add(membership);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Joined room successfully", roomId = room.Id });
    }

    /// <summary>
    /// Get pending join requests for a room (admin only)
    /// </summary>
    [HttpGet("{roomId}/join-requests")]
    public async Task<ActionResult<List<object>>> GetJoinRequests(string roomId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        // Check if user is admin
        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        var requests = await _dbContext.RoomJoinRequests
            .Where(jr => jr.RoomId == roomId && jr.Status == JoinRequestStatus.Pending)
            .Include(jr => jr.User)
            .OrderBy(jr => jr.CreatedAt)
            .ToListAsync();

        var result = requests.Select(jr => new
        {
            id = jr.Id,
            userId = jr.UserId,
            username = jr.User.UserName ?? jr.User.DisplayName,
            avatarUrl = jr.User.AvatarUrl,
            message = jr.Message,
            createdAt = jr.CreatedAt
        }).ToList<object>();

        return Ok(result);
    }

    /// <summary>
    /// Respond to join request (admin only)
    /// </summary>
    [HttpPost("join-requests/{requestId}/respond")]
    public async Task<IActionResult> RespondJoinRequest(string requestId, [FromBody] RespondJoinRequestDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var request = await _dbContext.RoomJoinRequests
            .Include(jr => jr.Room)
                .ThenInclude(r => r.Members)
            .FirstOrDefaultAsync(jr => jr.Id == requestId);

        if (request == null)
            return NotFound();

        // Check if user is admin
        var membership = request.Room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        if (model.Approve)
        {
            // Add as member
            var newMember = new RoomMember
            {
                UserId = request.UserId,
                RoomId = request.RoomId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.RoomMembers.Add(newMember);
            request.Status = JoinRequestStatus.Approved;
        }
        else
        {
            request.Status = JoinRequestStatus.Rejected;
        }

        request.RespondedAt = DateTime.UtcNow;
        request.RespondedByUserId = userId;

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = model.Approve ? "Request approved" : "Request rejected" });
    }

    ///// <summary>
    ///// Get room members
    ///// </summary>
    //[HttpGet("{roomId}/members")]
    //public async Task<ActionResult<List<RoomMemberDto>>> GetRoomMembers(string roomId)
    //{
    //    var userId = GetUserId();
    //    if (string.IsNullOrEmpty(userId))
    //        return Unauthorized();

    //    var room = await _dbContext.Rooms
    //        .Include(r => r.Members)
    //            .ThenInclude(m => m.User)
    //        .FirstOrDefaultAsync(r => r.Id == roomId);

    //    if (room == null)
    //        return NotFound();

    //    // Check if user is member
    //    if (!room.Members.Any(m => m.UserId == userId))
    //        return Forbid();

    //    var members = room.Members.Select(m => new RoomMemberDto
    //    {
    //        UserId = m.UserId,
    //        Username = m.User.UserName ?? m.User.DisplayName,
    //        AvatarUrl = m.User.AvatarUrl,
    //        IsAdmin = m.IsAdmin,
    //        IsCreator = m.UserId == room.CreatorId,
    //        IsOnline = _wsManager.IsUserOnline(m.UserId),
    //        JoinedAt = m.JoinedAt,
    //        LastSeen = m.User.LastSeen
    //    }).OrderByDescending(m => m.IsCreator)
    //      .ThenByDescending(m => m.IsAdmin)
    //      .ThenByDescending(m => m.IsOnline)
    //      .ToList();

    //    return Ok(members);
    //}

    /// <summary>
    /// Kick member from room (admin only)
    /// </summary>
    [HttpPost("{roomId}/kick")]
    public async Task<IActionResult> KickMember(string roomId, [FromBody] KickMemberDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        // Check if requester is admin
        var requesterMembership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (requesterMembership == null || !requesterMembership.IsAdmin)
            return Forbid();

        // Cannot kick creator
        if (model.UserId == room.CreatorId)
            return BadRequest(new { message = "Cannot kick room creator" });

        // Cannot kick yourself
        if (model.UserId == userId)
            return BadRequest(new { message = "Cannot kick yourself" });

        var targetMembership = room.Members.FirstOrDefault(m => m.UserId == model.UserId);
        if (targetMembership == null)
            return NotFound(new { message = "User is not a member" });

        _dbContext.RoomMembers.Remove(targetMembership);
        await _dbContext.SaveChangesAsync();

        // Notify via WebSocket
        await _wsManager.SendToUserAsync(model.UserId, new WebSocketMessage
        {
            Type = WebSocketMessageType.UserLeftRoom,
            Data = JsonSerializer.Serialize(new { roomId, kicked = true })
        });

        return Ok(new { message = "Member kicked successfully" });
    }

    /// <summary>
    /// Promote/demote admin (creator only)
    /// </summary>
    [HttpPost("{roomId}/promote-admin")]
    public async Task<IActionResult> PromoteAdmin(string roomId, [FromBody] PromoteAdminDto model)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        // Only creator can promote/demote admins
        if (room.CreatorId != userId)
            return Forbid();

        var targetMembership = room.Members.FirstOrDefault(m => m.UserId == model.UserId);
        if (targetMembership == null)
            return NotFound(new { message = "User is not a member" });

        targetMembership.IsAdmin = model.IsAdmin;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = model.IsAdmin ? "User promoted to admin" : "User demoted from admin" });
    }

    /// <summary>
    /// Upload room avatar
    /// </summary>
    [HttpPost("{roomId}/upload-avatar")]
    public async Task<ActionResult<string>> UploadRoomAvatar(string roomId, IFormFile file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        // Check if user is admin
        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        var uploaded = await _fileUploadService.UploadImageAsync(file, 500, 500);
        if (uploaded == null)
            return BadRequest(new { message = "Failed to upload image" });

        // Delete old avatar
        if (!string.IsNullOrEmpty(room.AvatarUrl))
        {
            await _fileUploadService.DeleteFileAsync(room.AvatarUrl);
        }

        room.AvatarUrl = uploaded.Url;
        await _dbContext.SaveChangesAsync();

        return Ok(new { avatarUrl = room.AvatarUrl });
    }

    /// <summary>
    /// Upload room cover image
    /// </summary>
    [HttpPost("{roomId}/upload-cover")]
    public async Task<ActionResult<string>> UploadRoomCover(string roomId, IFormFile file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        var uploaded = await _fileUploadService.UploadImageAsync(file, 1920, 500);
        if (uploaded == null)
            return BadRequest(new { message = "Failed to upload image" });

        if (!string.IsNullOrEmpty(room.CoverImageUrl))
        {
            await _fileUploadService.DeleteFileAsync(room.CoverImageUrl);
        }

        room.CoverImageUrl = uploaded.Url;
        await _dbContext.SaveChangesAsync();

        return Ok(new { coverImageUrl = room.CoverImageUrl });
    }

    /// <summary>
    /// Upload room background image
    /// </summary>
    [HttpPost("{roomId}/upload-background")]
    public async Task<ActionResult<string>> UploadRoomBackground(string roomId, IFormFile file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var room = await _dbContext.Rooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return NotFound();

        var membership = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null || !membership.IsAdmin)
            return Forbid();

        var uploaded = await _fileUploadService.UploadImageAsync(file, 1920, 1080);
        if (uploaded == null)
            return BadRequest(new { message = "Failed to upload image" });

        if (!string.IsNullOrEmpty(room.BackgroundImageUrl))
        {
            await _fileUploadService.DeleteFileAsync(room.BackgroundImageUrl);
        }

        room.BackgroundImageUrl = uploaded.Url;
        await _dbContext.SaveChangesAsync();

        return Ok(new { backgroundImageUrl = room.BackgroundImageUrl });
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }

    private string GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;

        do
        {
            code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        while (_dbContext.Rooms.Any(r => r.InviteCode == code));

        return code;
    }

    private RoomDto MapToDto(Room room, string? currentUserId = null)
    {
        var isAdmin = currentUserId != null && room.Members.Any(m => m.UserId == currentUserId && m.IsAdmin);
        var isCreator = currentUserId != null && room.CreatorId == currentUserId;

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
            ActiveMembersCount = room.Members.Count(m => _wsManager.IsUserOnline(m.UserId)),
            AvatarUrl = room.AvatarUrl,
            CoverImageUrl = room.CoverImageUrl,
            BackgroundImageUrl = room.BackgroundImageUrl,
            InviteCode = isAdmin ? room.InviteCode : null, // Only admins can see invite code
            AllowInviteLink = room.AllowInviteLink,
            RequireApproval = room.RequireApproval,
            AllowMemberInvite = room.AllowMemberInvite,
            MaxMembers = room.MaxMembers,
            CurrentMembersCount = room.Members.Count,
            IsAdmin = isAdmin,
            IsCreator = isCreator
        };
    }

    #endregion
}