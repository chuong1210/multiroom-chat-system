using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using ChatRoomSystem.Shared.Models;

namespace ChatRoomSystem.Client.Services;

/// <summary>
/// Service quản lý authentication state và token management
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthService> _logger;
    private const string TOKEN_KEY = "authToken";
    private const string USER_KEY = "currentUser";

    public UserDto? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    // Event khi authentication state thay đổi
    public event Action? OnAuthStateChanged;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    /// <summary>
    /// Initialize authentication state từ localStorage
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
            var user = await _localStorage.GetItemAsync<UserDto>(USER_KEY);

            if (!string.IsNullOrEmpty(token) && user != null)
            {
                CurrentUser = user;
                SetAuthHeader(token);
                _logger.LogInformation($"User {user.Username} authenticated from storage");
                OnAuthStateChanged?.Invoke();
            }
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop not available during prerendering - this is expected
            // Auth state will be restored after the circuit is established
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing auth state");
        }
    }

    /// <summary>
    /// Register user mới
    /// </summary>
    public async Task<(bool success, string message)> RegisterAsync(RegisterDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", model);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    await SetAuthenticatedAsync(authResponse.Token, authResponse.User);
                    return (true, "Đăng ký thành công!");
                }
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Registration failed: {errorMessage}");

            // Parse error message from response
            try
            {
                var error = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(errorMessage);
                if (error != null && error.ContainsKey("message"))
                {
                    return (false, error["message"].GetString() ?? "Đăng ký thất bại");
                }
            }
            catch { }

            return (false, "Đăng ký thất bại. Vui lòng thử lại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return (false, $"Lỗi: {ex.Message}");
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    public async Task<(bool success, string message)> LoginAsync(LoginDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", model);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    await SetAuthenticatedAsync(authResponse.Token, authResponse.User);
                    return (true, "Đăng nhập thành công!");
                }
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Login failed: {errorMessage}");

            // Parse error message
            try
            {
                var error = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(errorMessage);
                if (error != null && error.ContainsKey("message"))
                {
                    return (false, error["message"].GetString() ?? "Đăng nhập thất bại");
                }
            }
            catch { }

            return (false, "Email hoặc password không đúng");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return (false, $"Lỗi: {ex.Message}");
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            // Call API logout endpoint (optional)
            try
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling logout endpoint");
            }

            // Clear local storage
            await _localStorage.RemoveItemAsync(TOKEN_KEY);
            await _localStorage.RemoveItemAsync(USER_KEY);

            // Clear auth header
            _httpClient.DefaultRequestHeaders.Authorization = null;

            CurrentUser = null;

            _logger.LogInformation("User logged out");
            OnAuthStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    /// <summary>
    /// Get JWT token from storage
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<string>(TOKEN_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from storage");
            return null;
        }
    }

    /// <summary>
    /// Set authenticated state
    /// </summary>
    private async Task SetAuthenticatedAsync(string token, UserDto user)
    {
        await _localStorage.SetItemAsync(TOKEN_KEY, token);
        await _localStorage.SetItemAsync(USER_KEY, user);

        CurrentUser = user;
        SetAuthHeader(token);

        _logger.LogInformation($"User {user.Username} authenticated");
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Set Authorization header
    /// </summary>
    private void SetAuthHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Update current user info
    /// </summary>
    public async Task UpdateUserAsync(UserDto user)
    {
        CurrentUser = user;
        await _localStorage.SetItemAsync(USER_KEY, user);
        OnAuthStateChanged?.Invoke();
    }
}
