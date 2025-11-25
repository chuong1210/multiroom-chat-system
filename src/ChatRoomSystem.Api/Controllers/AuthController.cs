using ChatRoomSystem.Api.Services;
using ChatRoomSystem.Data;
using ChatRoomSystem.Data.Entities;
using ChatRoomSystem.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatRoomSystem.Api.Controllers;

/// <summary>
/// Controller cho Authentication (Login, Register)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtService _jwtService;
    private readonly ChatRoomDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService,
        ChatRoomDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký user mới
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email đã được sử dụng" });
        }

        // Check if username already exists
        existingUser = await _userManager.FindByNameAsync(model.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username đã được sử dụng" });
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            DisplayName = model.Username,
            IsOnline = false,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = $"Đăng ký thất bại: {errors}" });
        }

        _logger.LogInformation($"User {model.Username} registered successfully");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        // Create response
        var response = new AuthResponseDto
        {
            Token = token,
            ExpiresAt = _jwtService.GetTokenExpiry(),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen,
                AvatarUrl = user.AvatarUrl
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find user by email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Email hoặc password không đúng" });
        }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Email hoặc password không đúng" });
        }

        // Update online status và last seen
        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation($"User {user.UserName} logged in successfully");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        // Get friends list
        var friendIds = await GetUserFriendIdsAsync(user.Id);

        // Get rooms list
        var roomIds = await _dbContext.RoomMembers
            .Where(rm => rm.UserId == user.Id)
            .Select(rm => rm.RoomId)
            .ToListAsync();

        // Create response
        var response = new AuthResponseDto
        {
            Token = token,
            ExpiresAt = _jwtService.GetTokenExpiry(),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen,
                AvatarUrl = user.AvatarUrl,
                FriendIds = friendIds,
                RoomIds = roomIds
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get current user info (requires authentication)
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        // Get userId từ JWT token
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User không tồn tại" });
        }

        // Get friends list
        var friendIds = await GetUserFriendIdsAsync(user.Id);

        // Get rooms list
        var roomIds = await _dbContext.RoomMembers
            .Where(rm => rm.UserId == user.Id)
            .Select(rm => rm.RoomId)
            .ToListAsync();

        var response = new UserDto
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen,
            AvatarUrl = user.AvatarUrl,
            FriendIds = friendIds,
            RoomIds = roomIds
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout (update online status)
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation($"User {user.UserName} logged out");
            }
        }

        return Ok(new { message = "Đăng xuất thành công" });
    }

    /// <summary>
    /// Helper method để get friend IDs
    /// </summary>
    private async Task<List<string>> GetUserFriendIdsAsync(string userId)
    {
        var friendships = await _dbContext.Friendships
            .Where(f => f.User1Id == userId || f.User2Id == userId)
            .ToListAsync();

        var friendIds = friendships
            .Select(f => f.User1Id == userId ? f.User2Id : f.User1Id)
            .ToList();

        return friendIds;
    }
}
