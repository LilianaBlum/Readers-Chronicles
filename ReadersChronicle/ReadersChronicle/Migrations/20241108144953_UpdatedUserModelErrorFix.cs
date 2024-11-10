using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersChronicle.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUserModelErrorFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SequrityQuestion",
                table: "Users",
                newName: "SecurityQuestion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecurityQuestion",
                table: "Users",
                newName: "SequrityQuestion");
        }
    }
}
