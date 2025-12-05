using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ChatRoomSystem.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;

    public CustomAuthenticationStateProvider(AuthService authService)
    {
        _authService = authService;
        _authService.OnAuthStateChanged += OnAuthStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _authService.CurrentUser;

        if (user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        return new AuthenticationState(claimsPrincipal);
    }

    private void OnAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}