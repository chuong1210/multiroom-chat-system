// ChatRoomSystem.Shared/Models/RoomDto.cs
using System;
using System.Collections.Generic;

namespace ChatRoomSystem.Shared.Models;

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

    // ✅ NEW
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? InviteCode { get; set; }
    public bool AllowInviteLink { get; set; }
    public bool RequireApproval { get; set; }
    public bool AllowMemberInvite { get; set; }
    public int MaxMembers { get; set; }
    public int CurrentMembersCount { get; set; }
    public bool IsAdmin { get; set; } // Current user is admin
    public bool IsCreator { get; set; } // Current user is creator
}

public class CreateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public bool RequireApproval { get; set; }
    public bool AllowMemberInvite { get; set; } = true;
    public int MaxMembers { get; set; } = 200;
}

public class UpdateRoomDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public bool? IsPrivate { get; set; }
    public bool? RequireApproval { get; set; }
    public bool? AllowMemberInvite { get; set; }
    public bool? AllowInviteLink { get; set; }
    public int? MaxMembers { get; set; }
}

public class RoomMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsCreator { get; set; }
    public bool IsOnline { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastSeen { get; set; }
}

public class RoomPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int MembersCount { get; set; }
    public bool RequireApproval { get; set; }
    public bool IsFull { get; set; }
    public bool AlreadyMember { get; set; }
    public bool HasPendingRequest { get; set; }
}

public class JoinRoomRequestDto
{
    public string RoomId { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class RespondJoinRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public bool Approve { get; set; }
}

public class KickMemberDto
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class PromoteAdminDto
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class CreatePrivateRoomDto
{
    public string OtherUserId { get; set; } = string.Empty;
}