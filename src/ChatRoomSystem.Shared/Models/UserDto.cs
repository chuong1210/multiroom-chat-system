using System;
using System.Collections.Generic;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Data Transfer Object cho User
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> FriendIds { get; set; } = new();
    public List<string> RoomIds { get; set; } = new();
}

/// <summary>
/// DTO cho đăng ký user mới
/// </summary>
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho đăng nhập
/// </summary>
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho response sau khi login thành công
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
