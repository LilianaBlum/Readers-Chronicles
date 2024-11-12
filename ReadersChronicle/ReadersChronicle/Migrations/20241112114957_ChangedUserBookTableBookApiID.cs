using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersChronicle.Migrations
{
    /// <inheritdoc />
    public partial class ChangedUserBookTableBookApiID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BookApiID",
                table: "UserBooks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BookApiID",
                table: "UserBooks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
