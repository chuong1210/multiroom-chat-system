using System.Net.Http.Json;
using ChatRoomSystem.Shared.Models;

namespace ChatRoomSystem.Client.Services;

/// <summary>
/// Tổng hợp tất cả API calls vào một service duy nhất
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    #region Rooms

    public async Task<List<RoomDto>> GetPublicRoomsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoomDto>>("/api/rooms/public") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public rooms");
            return new();
        }
    }

    public async Task<List<RoomDto>> GetMyRoomsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoomDto>>("/api/rooms/my-rooms") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my rooms");
            return new();
        }
    }

    public async Task<RoomDto?> GetRoomAsync(string roomId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RoomDto>($"/api/rooms/{roomId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting room {roomId}");
            return null;
        }
    }

    public async Task<RoomDto?> CreateRoomAsync(CreateRoomDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/rooms", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoomDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return null;
        }
    }

    public async Task<bool> JoinRoomAsync(string roomId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/join", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error joining room {roomId}");
            return false;
        }
    }

    public async Task<bool> LeaveRoomAsync(string roomId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/leave", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error leaving room {roomId}");
            return false;
        }
    }

    public async Task<List<RoomDto>> SearchRoomsAsync(string query)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoomDto>>($"/api/rooms/search?query={query}") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching rooms");
            return new();
        }
    }

    #endregion

    #region Messages

    public async Task<MessagePageDto?> GetRoomMessagesAsync(string roomId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MessagePageDto>(
                $"/api/messages/room/{roomId}?pageNumber={pageNumber}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting messages for room {roomId}");
            return null;
        }
    }

    #endregion

    #region Friends

    public async Task<List<UserDto>> GetFriendsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserDto>>("/api/friends") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends");
            return new();
        }
    }

    public async Task<List<FriendRequestDto>> GetFriendRequestsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<FriendRequestDto>>("/api/friends/requests") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friend requests");
            return new();
        }
    }

    public async Task<bool> SendFriendRequestAsync(string toUserId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/friends/request",
                new SendFriendRequestDto { ToUserId = toUserId });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request");
            return false;
        }
    }

    public async Task<bool> RespondFriendRequestAsync(string requestId, bool accept)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond",
                new RespondFriendRequestDto { RequestId = requestId, Accept = accept });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to friend request");
            return false;
        }
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserDto>>($"/api/friends/search?query={query}") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return new();
        }
    }

    #endregion

    #region Tasks

    public async Task<List<TaskDto>> GetRoomTasksAsync(string roomId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TaskDto>>($"/api/tasks/room/{roomId}") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting tasks for room {roomId}");
            return new();
        }
    }

    public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tasks", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TaskDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return null;
        }
    }

    public async Task<TaskDto?> UpdateTaskAsync(string taskId, UpdateTaskDto model)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/tasks/{taskId}", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TaskDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating task {taskId}");
            return null;
        }
    }

    public async Task<bool> DeleteTaskAsync(string taskId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/tasks/{taskId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting task {taskId}");
            return false;
        }
    }

    public async Task<TaskCommentDto?> AddTaskCommentAsync(string taskId, string content)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/tasks/{taskId}/comments",
                new AddTaskCommentDto { TaskId = taskId, Content = content });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TaskCommentDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding comment to task {taskId}");
            return null;
        }
    }

    #endregion
}
