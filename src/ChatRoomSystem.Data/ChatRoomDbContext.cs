using ChatRoomSystem.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatRoomSystem.Data;

/// <summary>
/// DbContext cho Chat Room System, extends IdentityDbContext để sử dụng ASP.NET Identity
/// </summary>
public class ChatRoomDbContext : IdentityDbContext<ApplicationUser>
{
    public ChatRoomDbContext(DbContextOptions<ChatRoomDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomMember> RoomMembers { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<FriendRequest> FriendRequests { get; set; } = null!;
    public DbSet<RoomInvitation> RoomInvitations { get; set; } = null!;
    public DbSet<ChatRoomTask> Tasks { get; set; } = null!;
    public DbSet<TaskComment> TaskComments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
        });

        // Configure Room
        builder.Entity<Room>(entity =>
        {
            entity.HasOne(r => r.Creator)
                .WithMany(u => u.CreatedRooms)
                .HasForeignKey(r => r.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.Name);
            entity.HasIndex(r => r.CreatedAt);
        });

        // Configure RoomMember (many-to-many relationship giữa User và Room)
        builder.Entity<RoomMember>(entity =>
        {
            entity.HasOne(rm => rm.User)
                .WithMany(u => u.RoomMemberships)
                .HasForeignKey(rm => rm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rm => rm.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(rm => rm.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rm => new { rm.UserId, rm.RoomId }).IsUnique();
        });

        // Configure Message
        builder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.RoomId);
            entity.HasIndex(m => m.Timestamp);
        });

        // Configure Friendship (many-to-many relationship giữa Users)
        builder.Entity<Friendship>(entity =>
        {
            entity.HasOne(f => f.User1)
                .WithMany(u => u.FriendsInitiated)
                .HasForeignKey(f => f.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.User2)
                .WithMany(u => u.FriendsReceived)
                .HasForeignKey(f => f.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => new { f.User1Id, f.User2Id }).IsUnique();
        });

        // Configure FriendRequest
        builder.Entity<FriendRequest>(entity =>
        {
            entity.HasOne(fr => fr.FromUser)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(fr => fr.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fr => fr.ToUser)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(fr => new { fr.FromUserId, fr.ToUserId, fr.Status });
        });

        // Configure RoomInvitation
        builder.Entity<RoomInvitation>(entity =>
        {
            entity.HasOne(ri => ri.Room)
                .WithMany(r => r.Invitations)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ri => ri.InvitedUser)
                .WithMany()
                .HasForeignKey(ri => ri.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ri => ri.InvitedByUser)
                .WithMany()
                .HasForeignKey(ri => ri.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ri => new { ri.RoomId, ri.InvitedUserId, ri.Status });
        });

        // Configure ChatRoomTask
        builder.Entity<ChatRoomTask>(entity =>
        {
            entity.HasOne(t => t.Room)
                .WithMany(r => r.Tasks)
                .HasForeignKey(t => t.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.RoomId, t.Status, t.BoardPosition });
        });

        // Configure TaskComment
        builder.Entity<TaskComment>(entity =>
        {
            entity.HasOne(tc => tc.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tc => tc.User)
                .WithMany()
                .HasForeignKey(tc => tc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(tc => new { tc.TaskId, tc.CreatedAt });
        });
    }
}
