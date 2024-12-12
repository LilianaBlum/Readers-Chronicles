using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersChronicle.Migrations
{
    /// <inheritdoc />
    public partial class UserFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_UserID1",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_UserID2",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "FriendshipStatus",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "UserID1",
                table: "Friendships",
                newName: "UserId1");

            migrationBuilder.RenameColumn(
                name: "UserID2",
                table: "Friendships",
                newName: "User2ID");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_UserID1",
                table: "Friendships",
                newName: "IX_Friendships_UserId1");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_UserID2",
                table: "Friendships",
                newName: "IX_Friendships_User2ID");

            migrationBuilder.AlterColumn<string>(
                name: "UserId1",
                table: "Friendships",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "User1ID",
                table: "Friendships",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Friendships",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingFriendships",
                columns: table => new
                {
                    FriendshipID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiatorUserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovingUserID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingFriendships", x => x.FriendshipID);
                    table.ForeignKey(
                        name: "FK_PendingFriendships_AspNetUsers_ApprovingUserID",
                        column: x => x.ApprovingUserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingFriendships_AspNetUsers_InitiatorUserID",
                        column: x => x.InitiatorUserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_User1ID",
                table: "Friendships",
                column: "User1ID");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserId",
                table: "Friendships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingFriendships_ApprovingUserID",
                table: "PendingFriendships",
                column: "ApprovingUserID");

            migrationBuilder.CreateIndex(
                name: "IX_PendingFriendships_InitiatorUserID",
                table: "PendingFriendships",
                column: "InitiatorUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_User1ID",
                table: "Friendships",
                column: "User1ID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_User2ID",
                table: "Friendships",
                column: "User2ID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_UserId",
                table: "Friendships",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_UserId1",
                table: "Friendships",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_User1ID",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_User2ID",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_UserId",
                table: "Friendships");

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_AspNetUsers_UserId1",
                table: "Friendships");

            migrationBuilder.DropTable(
                name: "PendingFriendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_User1ID",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_UserId",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "User1ID",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Friendships");

            migrationBuilder.RenameColumn(
                name: "UserId1",
                table: "Friendships",
                newName: "UserID1");

            migrationBuilder.RenameColumn(
                name: "User2ID",
                table: "Friendships",
                newName: "UserID2");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_UserId1",
                table: "Friendships",
                newName: "IX_Friendships_UserID1");

            migrationBuilder.RenameIndex(
                name: "IX_Friendships_User2ID",
                table: "Friendships",
                newName: "IX_Friendships_UserID2");

            migrationBuilder.AlterColumn<string>(
                name: "UserID1",
                table: "Friendships",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FriendshipStatus",
                table: "Friendships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_UserID1",
                table: "Friendships",
                column: "UserID1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_AspNetUsers_UserID2",
                table: "Friendships",
                column: "UserID2",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
