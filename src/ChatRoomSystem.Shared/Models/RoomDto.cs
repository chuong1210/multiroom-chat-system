using System;
using System.Collections.Generic;

namespace ChatRoomSystem.Shared.Models;

/// <summary>
/// Data Transfer Object cho Room/Group
/// </summary>
public class RoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string CreatorId { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> MemberIds { get; set; } = new();
    public int ActiveMembersCount { get; set; }
}

/// <summary>
/// DTO cho tạo room mới
/// </summary>
public class CreateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
}

/// <summary>
/// DTO cho cập nhật room
/// </summary>
public class UpdateRoomDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public bool? IsPrivate { get; set; }
}

/// <summary>
/// DTO cho lời mời vào room
/// </summary>
public class RoomInviteDto
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public string InvitedByUsername { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
}
