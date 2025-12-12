using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho User operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ChatRoomDbContext _dbContext;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ChatRoomDbContext dbContext,
        WebSocketConnectionManager wsManager,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _wsManager = wsManager;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(string userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User không tồn tại" });
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            IsOnline = _wsManager.IsUserOnline(user.Id),
            LastSeen = user.LastSeen,
            AvatarUrl = user.AvatarUrl
        };

        return Ok(userDto);
    }
}