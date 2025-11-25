using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatRoomSystem.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ChatRoomSystem.Api.Services;

/// <summary>
/// Service để generate và validate JWT tokens
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generate JWT token cho user
    /// </summary>
    public string GenerateToken(ApplicationUser user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? "YourSuperSecretKeyHereShouldBeAtLeast32CharactersLong!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "ChatRoomSystem";
        var audience = _configuration["JwtSettings:Audience"] ?? "ChatRoomSystem";
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440"); // Default 24h

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim("userId", user.Id),
            new Claim("username", user.UserName ?? user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation($"Generated JWT token for user {user.Id} ({user.UserName})");

        return tokenString;
    }

    /// <summary>
    /// Validate JWT token và return userId
    /// </summary>
    public string? ValidateToken(string token)
    {
        try
        {
            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? "YourSuperSecretKeyHereShouldBeAtLeast32CharactersLong!";
            var issuer = _configuration["JwtSettings:Issuer"] ?? "ChatRoomSystem";
            var audience = _configuration["JwtSettings:Audience"] ?? "ChatRoomSystem";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userId = principal.FindFirst("userId")?.Value
                      ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    /// <summary>
    /// Get expiry time cho token
    /// </summary>
    public DateTime GetTokenExpiry()
    {
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440");
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }
}
