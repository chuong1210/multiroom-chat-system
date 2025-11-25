using System;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Enum cho trạng thái friend request
/// </summary>
public enum FriendRequestStatus
{
    Pending,   // Đang chờ xử lý
    Accepted,  // Đã chấp nhận
    Rejected   // Đã từ chối
}

/// <summary>
/// DTO cho Friend Request
/// </summary>
public class FriendRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string FromUsername { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public string ToUsername { get; set; } = string.Empty;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// DTO cho gửi friend request
/// </summary>
public class SendFriendRequestDto
{
    public string ToUserId { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho response friend request
/// </summary>
public class RespondFriendRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public bool Accept { get; set; }
}
