using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersChronicle.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedProfileModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePicture",
                table: "Profiles",
                newName: "ImageMimeType");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Profiles",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Profiles");

            migrationBuilder.RenameColumn(
                name: "ImageMimeType",
                table: "Profiles",
                newName: "ProfilePicture");
        }
    }
}
