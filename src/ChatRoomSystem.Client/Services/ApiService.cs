// ChatRoomSystem.Client/Services/ApiService.cs
using System.Net.Http.Json;
using ChatRoomSystem.Shared.Models;
using Microsoft.Extensions.Logging;

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

    public async Task<RoomDto?> UpdateRoomAsync(string roomId, UpdateRoomDto model)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/rooms/{roomId}", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoomDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating room {roomId}");
            return null;
        }
    }

    // ChatRoomSystem.Client/Services/ApiService.cs

    public async Task<bool> JoinRoomAsync(string roomId)
    {
        try
        {
            _logger.LogInformation($"🔵 Attempting to join room {roomId}");

            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/join", null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"✅ Successfully joined room {roomId}: {content}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"⚠️ Failed to join room {roomId}: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"🔴 Error joining room {roomId}");
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
            return await _httpClient.GetFromJsonAsync<List<RoomDto>>($"/api/rooms/search?query={query}") ?? new();  // ✅ FIX: Đổi SchemaConverter() → new()
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching rooms");
            return new();
        }
    }

    #endregion

    #region Room Management

    /// <summary>
    /// Generate invite code for room
    /// </summary>
    public async Task<(string inviteCode, string inviteUrl)?> GenerateInviteCodeAsync(string roomId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/generate-invite-code", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (result != null)
                {
                    return (result["inviteCode"], result["inviteUrl"]);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invite code");
            return null;
        }
    }

    /// <summary>
    /// Get room preview by invite code
    /// </summary>
    public async Task<RoomPreviewDto?> GetRoomPreviewAsync(string inviteCode)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RoomPreviewDto>($"/api/rooms/preview/{inviteCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room preview");
            return null;
        }
    }

    /// <summary>
    /// Join room by invite code
    /// </summary>
    public async Task<(bool success, string message, bool requiresApproval)> JoinRoomByCodeAsync(string inviteCode, string? message = null)
    {
        try
        {
            var model = new JoinRoomRequestDto { RoomId = "", Message = message };
            var response = await _httpClient.PostAsJsonAsync($"/api/rooms/join-by-code/{inviteCode}", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                var requiresApproval = result?.ContainsKey("requiresApproval") == true && (bool)result["requiresApproval"];
                var msg = result?["message"]?.ToString() ?? "Success";
                return (true, msg, requiresApproval);
            }

            var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return (false, error?["message"] ?? "Failed to join", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room by code");
            return (false, "Error joining room", false);
        }
    }

    /// <summary>
    /// Get room members
    /// </summary>
    public async Task<List<RoomMemberDto>> GetRoomMembersAsync(string roomId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RoomMemberDto>>($"/api/rooms/{roomId}/members") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room members");
            return new();
        }
    }

    /// <summary>
    /// Get pending join requests
    /// </summary>
    public async Task<List<object>> GetJoinRequestsAsync(string roomId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<object>>($"/api/rooms/{roomId}/join-requests") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting join requests");
            return new();
        }
    }

    /// <summary>
    /// Respond to join request
    /// </summary>
    public async Task<bool> RespondJoinRequestAsync(string requestId, bool approve)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/rooms/join-requests/{requestId}/respond",
                new RespondJoinRequestDto { RequestId = requestId, Approve = approve });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to join request");
            return false;
        }
    }

    /// <summary>
    /// Kick member from room
    /// </summary>
    public async Task<bool> KickMemberAsync(string roomId, string userId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/rooms/{roomId}/kick",
                new KickMemberDto { RoomId = roomId, UserId = userId });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error kicking member");
            return false;
        }
    }

    /// <summary>
    /// Promote/demote admin
    /// </summary>
    public async Task<bool> PromoteAdminAsync(string roomId, string userId, bool isAdmin)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/rooms/{roomId}/promote-admin",
                new PromoteAdminDto { RoomId = roomId, UserId = userId, IsAdmin = isAdmin });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting admin");
            return false;
        }
    }

    /// <summary>
    /// Upload room avatar
    /// </summary>
    public async Task<string?> UploadRoomAvatarAsync(string roomId, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/upload-avatar", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["avatarUrl"];
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading room avatar");
            return null;
        }
    }

    /// <summary>
    /// Upload room cover
    /// </summary>
    public async Task<string?> UploadRoomCoverAsync(string roomId, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/upload-cover", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["coverImageUrl"];
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading room cover");
            return null;
        }
    }

    /// <summary>
    /// Upload room background
    /// </summary>
    public async Task<string?> UploadRoomBackgroundAsync(string roomId, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/rooms/{roomId}/upload-background", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["backgroundImageUrl"];
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading room background");
            return null;
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

    // <summary>
    /// Upload file and send as message
    /// ✅ FIXED: Handle empty/invalid content types
    /// </summary>
    public async Task<MessageDto?> UploadFileMessageAsync(
        string roomId,
        Stream fileStream,
        string fileName,
        string? contentType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(roomId), "roomId");

            var fileContent = new StreamContent(fileStream);

            // ✅ FIX: Validate and set proper content type
            if (string.IsNullOrWhiteSpace(contentType))
            {
                // Detect content type from file extension
                contentType = GetContentTypeFromFileName(fileName);
                _logger.LogInformation($"📄 Auto-detected content type: {contentType} for {fileName}");
            }

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            _logger.LogInformation($"📤 Uploading file: {fileName} ({contentType})");

            var response = await _httpClient.PostAsync("/api/messages/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadFromJsonAsync<MessageDto>();
                _logger.LogInformation($"✅ File uploaded successfully: {fileName}");
                return message;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"❌ Upload failed: {response.StatusCode} - {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error uploading file message: {fileName}");
            return null;
        }
    
    }

    /// <summary>
    /// Get content type from file extension
    /// </summary>
    private string GetContentTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            // Documents
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

            // Text files
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",

            // Database files
            ".sql" => "application/sql",
            ".db" => "application/x-sqlite3",
            ".sqlite" => "application/x-sqlite3",

            // Code files
            ".cs" => "text/x-csharp",
            ".java" => "text/x-java",
            ".py" => "text/x-python",
            ".cpp" => "text/x-c++src",
            ".c" => "text/x-csrc",
            ".h" => "text/x-chdr",

            // Images
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",

            // Videos
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",

            // Audio
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",

            // Archives
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",

            // Other
            ".apk" => "application/vnd.android.package-archive",
            ".exe" => "application/x-msdownload",
            ".dll" => "application/x-msdownload",

            // Default
            _ => "application/octet-stream"
        };
    }
    /// <summary>
    /// Upload image and send as message
    /// </summary>
    public async Task<MessageDto?> UploadImageMessageAsync(string roomId, Stream imageStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(roomId), "roomId");

            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "file", fileName);

            var response = await _httpClient.PostAsync("/api/messages/upload-image", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image message");
            return null;
        }
    }

    #endregion

    #region Private Messages (1-1 Chat)

    /// <summary>
    /// Get user info by ID
    /// </summary>
    public async Task<UserDto?> GetUserAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>($"/api/users/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user {userId}");
            return null;
        }
    }

    /// <summary>
    /// Get private messages between current user and another user
    /// </summary>
    // ApiService.cs - Add pagination overload
    public async Task<List<MessageDto>> GetPrivateMessagesAsync(string otherUserId, int page = 1, int pageSize = 50)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<MessageDto>>(
                $"/api/messages/private/{otherUserId}?page={page}&pageSize={pageSize}") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting private messages with user {otherUserId}");
            return new();
        }
    }

    /// <summary>
    /// Get or create private room with another user
    /// </summary>
    public async Task<string?> GetOrCreatePrivateRoomAsync(string otherUserId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/rooms/private", new { OtherUserId = otherUserId });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return result?["roomId"];
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating private room with user {otherUserId}");
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

    #region Notifications

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return new();
        }
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<int>("/api/notifications/unread-count");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count");
            return 0;
        }
    }

    public async Task<bool> MarkNotificationAsReadAsync(string notificationId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/notifications/{notificationId}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {notificationId} as read");
            return false;
        }
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/notifications/read-all", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return false;
        }
    }

    public async Task<bool> DismissNotificationAsync(string notificationId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/notifications/{notificationId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error dismissing notification {notificationId}");
            return false;
        }
    }

    #endregion

    #region Conversations

    /// <summary>
    /// Get all conversations (private chats)
    /// </summary>
    public async Task<List<ConversationDto>> GetConversationsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ConversationDto>>("/api/messages/conversations") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return new();
        }
    }

    /// <summary>
    /// Get unread messages count
    /// </summary>
    public async Task<int> GetUnreadMessagesCountAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<int>("/api/messages/unread-count");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread messages count");
            return 0;
        }
    }

    /// <summary>
    /// Mark conversation as read
    /// </summary>
    public async Task<bool> MarkConversationAsReadAsync(string otherUserId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/messages/conversations/{otherUserId}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking conversation with {otherUserId} as read");
            return false;
        }
    }

    #endregion
}