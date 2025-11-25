using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChatRoomSystem.Data.Entities;

/// <summary>
/// Entity cho User, extends IdentityUser để sử dụng ASP.NET Identity
/// </summary>
public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Danh sách bạn bè (relationship nhiều-nhiều)
    /// </summary>
    public virtual ICollection<Friendship> FriendsInitiated { get; set; } = new List<Friendship>();
    public virtual ICollection<Friendship> FriendsReceived { get; set; } = new List<Friendship>();

    /// <summary>
    /// Friend requests đã gửi
    /// </summary>
    public virtual ICollection<FriendRequest> SentFriendRequests { get; set; } = new List<FriendRequest>();

    /// <summary>
    /// Friend requests đã nhận
    /// </summary>
    public virtual ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();

    /// <summary>
    /// Rooms mà user đã tạo
    /// </summary>
    public virtual ICollection<Room> CreatedRooms { get; set; } = new List<Room>();

    /// <summary>
    /// Room memberships
    /// </summary>
    public virtual ICollection<RoomMember> RoomMemberships { get; set; } = new List<RoomMember>();

    /// <summary>
    /// Messages đã gửi
    /// </summary>
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();

    /// <summary>
    /// Tasks được assign
    /// </summary>
    public virtual ICollection<ChatRoomTask> AssignedTasks { get; set; } = new List<ChatRoomTask>();

    /// <summary>
    /// Tasks đã tạo
    /// </summary>
    public virtual ICollection<ChatRoomTask> CreatedTasks { get; set; } = new List<ChatRoomTask>();
}
