// ChatRoomSystem.Api/Controllers/MessagesController.cs
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
using System.Text.Json;
using System.Threading.Tasks;

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
    private readonly WebSocketConnectionManager _wsManager;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        IFileUploadService fileUploadService,
        ILogger<MessagesController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Get all conversations (private chats) for current user
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get all private rooms where user is a member
        var privateRooms = await _dbContext.Rooms
            .Where(r => r.IsPrivate && r.Members.Any(m => m.UserId == userId))
            .Include(r => r.Members)
                .ThenInclude(m => m.User)
            .ToListAsync();

        var conversations = new List<ConversationDto>();

        foreach (var room in privateRooms)
        {
            // Get the other user in this private room
            var otherMember = room.Members.FirstOrDefault(m => m.UserId != userId);
            if (otherMember == null) continue;

            // Get current user's membership to check LastReadAt
            var currentUserMembership = room.Members.FirstOrDefault(m => m.UserId == userId);
            var lastReadAt = currentUserMembership?.LastReadAt ?? DateTime.MinValue;

            // Get last message
            var lastMessage = await _dbContext.Messages
                .Where(m => m.RoomId == room.Id && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync();

            // Count unread messages (messages from OTHER user after MY LastReadAt)
            var unreadCount = await _dbContext.Messages
                .Where(m => m.RoomId == room.Id
                    && m.SenderId != userId  // Messages from other user
                    && !m.IsDeleted
                    && m.Timestamp > lastReadAt)  // After my last read
                .CountAsync();

            conversations.Add(new ConversationDto
            {
                UserId = otherMember.UserId,
                Username = otherMember.User.UserName ?? otherMember.User.DisplayName,
                AvatarUrl = otherMember.User.AvatarUrl,
                LastMessage = lastMessage?.Content ?? "No messages yet",
                LastMessageTime = lastMessage?.Timestamp ?? room.CreatedAt,
                UnreadCount = unreadCount,
                IsOnline = _wsManager.IsUserOnline(otherMember.UserId),
                IsTyping = false,
                HasUnread = unreadCount > 0,
                RoomId = room.Id
            });
        }

        // Sort by last message time (most recent first)
        conversations = conversations
            .OrderByDescending(c => c.LastMessageTime)
            .ToList();

        return Ok(conversations);
    }

    /// <summary>
    /// Get unread messages count across all conversations
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadMessagesCount()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get all private rooms
        var privateRoomIds = await _dbContext.Rooms
            .Where(r => r.IsPrivate && r.Members.Any(m => m.UserId == userId))
            .Select(r => r.Id)
            .ToListAsync();

        // Get user's last read timestamps for each room
        var lastReadTimes = await _dbContext.RoomMembers
            .Where(rm => rm.UserId == userId && privateRoomIds.Contains(rm.RoomId))
            .ToDictionaryAsync(rm => rm.RoomId, rm => rm.LastReadAt ?? DateTime.MinValue);

        // Count unread messages
        var unreadCount = 0;
        foreach (var roomId in privateRoomIds)
        {
            var lastReadTime = lastReadTimes.GetValueOrDefault(roomId, DateTime.MinValue);

            var count = await _dbContext.Messages
                .Where(m => m.RoomId == roomId
                    && m.SenderId != userId
                    && m.Timestamp > lastReadTime)
                .CountAsync();

            unreadCount += count;
        }

        return Ok(unreadCount);
    }

    /// <summary>
    /// Mark conversation as read (update LastReadAt)
    /// </summary>
    [HttpPost("conversations/{otherUserId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(string otherUserId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Find private room
        var privateRoom = await _dbContext.Rooms
            .Where(r => r.IsPrivate && r.Members.Count == 2)
            .Where(r => r.Members.Any(m => m.UserId == userId) &&
                       r.Members.Any(m => m.UserId == otherUserId))
            .FirstOrDefaultAsync();

        if (privateRoom == null)
        {
            return NotFound(new { message = "Conversation không tồn tại" });
        }

        // Update LastReadAt
        var membership = await _dbContext.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == privateRoom.Id && rm.UserId == userId);

        if (membership != null)
        {
            membership.LastReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { message = "Conversation marked as read" });
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
            FileSize = m.FileSize,
            FileMimeType = m.FileMimeType,
            ThumbnailUrl = m.ThumbnailUrl,
            MediaWidth = m.MediaWidth,
            MediaHeight = m.MediaHeight,
            MediaDuration = m.MediaDuration
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // ✅ Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50; // Max 100 messages per request

        try
        {
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

            // ✅ Calculate skip amount for pagination
            var skip = (page - 1) * pageSize;

            // Get messages with pagination
            // ⚠️ IMPORTANT: OrderByDescending để lấy messages mới nhất trước
            // Sau đó skip old messages và take pageSize
            // Cuối cùng reverse để hiển thị theo thứ tự chronological
            var messages = await _dbContext.Messages
                .Where(m => m.RoomId == privateRoom.Id && !m.IsDeleted)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Timestamp) // Newest first
                .Skip(skip)                          // Skip already loaded pages
                .Take(pageSize)                      // Take current page
                .ToListAsync();

            // ✅ Reverse to chronological order (oldest → newest)
            messages.Reverse();

            // Map to DTOs
            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                RoomId = m.RoomId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender.UserName ?? m.Sender.DisplayName,
                Content = m.Content,
                Type = m.Type,
                Timestamp = m.Timestamp,

                // ✅ File/Media properties
                FileUrl = m.FileUrl,
                FileName = m.FileName,
                FileSize = m.FileSize,
                FileMimeType = m.FileMimeType,
                ThumbnailUrl = m.ThumbnailUrl,
                MediaWidth = m.MediaWidth,
                MediaHeight = m.MediaHeight,
                MediaDuration = m.MediaDuration,

                // ✅ Optional: Add IsRead status
                IsRead = m.IsRead
            }).ToList();

            _logger.LogInformation(
                $"Retrieved {messageDtos.Count} messages for room {privateRoom.Id} (Page {page}, PageSize {pageSize})");

            return Ok(messageDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting private messages with user {otherUserId}");
            return StatusCode(500, new { message = "Failed to retrieve messages" });
        }
    }

    /// <summary>
    /// Upload file and send as message
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<MessageDto>> UploadFile([FromForm] string roomId, [FromForm] IFormFile file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Check if user is member
        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!isMember)
            return Forbid();

        // Upload file
        UploadedFileDto? uploadedFile;

        if (file.ContentType.StartsWith("image/"))
        {
            uploadedFile = await _fileUploadService.UploadImageAsync(file);
        }
        else if (file.ContentType.StartsWith("video/"))
        {
            uploadedFile = await _fileUploadService.UploadVideoAsync(file);
        }
        else
        {
            uploadedFile = await _fileUploadService.UploadFileAsync(file, "files");
        }

        if (uploadedFile == null)
            return BadRequest(new { message = "Failed to upload file" });

        // Create message
        var message = new Message
        {
            RoomId = roomId,
            SenderId = userId,
            Content = file.FileName, // Original filename as content
            Type = GetMessageType(uploadedFile.FileType),
            FileUrl = uploadedFile.Url,
            FileName = uploadedFile.FileName,
            FileSize = uploadedFile.FileSize,
            FileMimeType = uploadedFile.MimeType,
            ThumbnailUrl = uploadedFile.ThumbnailUrl,
            MediaWidth = uploadedFile.Width,
            MediaHeight = uploadedFile.Height,
            MediaDuration = uploadedFile.Duration,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        // Load sender info
        var sender = await _dbContext.Users.FindAsync(userId);

        var messageDto = new MessageDto
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = message.SenderId,
            SenderUsername = sender?.UserName ?? sender?.DisplayName ?? "Unknown",
            Content = message.Content,
            Type = message.Type,
            Timestamp = message.Timestamp,
            FileUrl = message.FileUrl,
            FileName = message.FileName,
            FileSize = message.FileSize,
            FileMimeType = message.FileMimeType,
            ThumbnailUrl = message.ThumbnailUrl,
            MediaWidth = message.MediaWidth,
            MediaHeight = message.MediaHeight,
            MediaDuration = message.MediaDuration
        };

        // Broadcast via WebSocket
        await _wsManager.BroadcastToRoomAsync(roomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.MessageReceived,
            Data = JsonSerializer.Serialize(messageDto)
        });

        return Ok(messageDto);
    }

    /// <summary>
    /// Upload image and send as message
    /// </summary>
    [HttpPost("upload-image")]
    public async Task<ActionResult<MessageDto>> UploadImage([FromForm] string roomId, [FromForm] IFormFile file)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var isMember = await _dbContext.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId);

        if (!isMember)
            return Forbid();

        var uploadedFile = await _fileUploadService.UploadImageAsync(file);
        if (uploadedFile == null)
            return BadRequest(new { message = "Failed to upload image" });

        var message = new Message
        {
            RoomId = roomId,
            SenderId = userId,
            Content = "Image",
            Type = MessageType.Image,
            FileUrl = uploadedFile.Url,
            FileName = uploadedFile.FileName,
            FileSize = uploadedFile.FileSize,
            FileMimeType = uploadedFile.MimeType,
            ThumbnailUrl = uploadedFile.ThumbnailUrl,
            MediaWidth = uploadedFile.Width,
            MediaHeight = uploadedFile.Height,
            Timestamp = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        var sender = await _dbContext.Users.FindAsync(userId);

        var messageDto = new MessageDto
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = message.SenderId,
            SenderUsername = sender?.UserName ?? sender?.DisplayName ?? "Unknown",
            Content = message.Content,
            Type = message.Type,
            Timestamp = message.Timestamp,
            FileUrl = message.FileUrl,
            FileName = message.FileName,
            FileSize = message.FileSize,
            FileMimeType = message.FileMimeType,
            ThumbnailUrl = message.ThumbnailUrl,
            MediaWidth = message.MediaWidth,
            MediaHeight = message.MediaHeight
        };

        await _wsManager.BroadcastToRoomAsync(roomId, new WebSocketMessage
        {
            Type = WebSocketMessageType.MessageReceived,
            Data = JsonSerializer.Serialize(messageDto)
        });

        return Ok(messageDto);
    }

    private string? GetUserId()
    {
        return User.FindFirst("userId")?.Value;
    }

    private MessageType GetMessageType(FileType fileType)
    {
        return fileType switch
        {
            FileType.Image => MessageType.Image,
            FileType.Video => MessageType.Video,
            FileType.Audio => MessageType.Audio,
            FileType.Document => MessageType.File,
            _ => MessageType.File
        };
    }
}