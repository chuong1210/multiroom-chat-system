using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatRoomSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowInviteLink",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMemberInvite",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Rooms",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundImageUrl",
                table: "Rooms",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Rooms",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Rooms",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxMembers",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireApproval",
                table: "Rooms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FileMimeType",
                table: "Messages",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaDuration",
                table: "Messages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaHeight",
                table: "Messages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaWidth",
                table: "Messages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Messages",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomJoinRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    RoomId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RespondedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomJoinRequests_AspNetUsers_RespondedByUserId",
                        column: x => x.RespondedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomJoinRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomJoinRequests_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_InviteCode",
                table: "Rooms",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomJoinRequests_RespondedByUserId",
                table: "RoomJoinRequests",
                column: "RespondedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomJoinRequests_RoomId_UserId_Status",
                table: "RoomJoinRequests",
                columns: new[] { "RoomId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomJoinRequests_UserId",
                table: "RoomJoinRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_InviteCode",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "AllowInviteLink",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "AllowMemberInvite",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "BackgroundImageUrl",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MaxMembers",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RequireApproval",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "FileMimeType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaDuration",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaHeight",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaWidth",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Messages");
        }
    }
}
