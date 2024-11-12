using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadersChronicle.Migrations
{
    /// <inheritdoc />
    public partial class ChangedUserBookTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "UserBooks");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "UserBooks");

            migrationBuilder.RenameColumn(
                name: "Folder",
                table: "UserBooks",
                newName: "PictureMimeType");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "UserBooks",
                newName: "Length");

            migrationBuilder.AddColumn<int>(
                name: "BookApiID",
                table: "UserBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "Picture",
                table: "UserBooks",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookApiID",
                table: "UserBooks");

            migrationBuilder.DropColumn(
                name: "Picture",
                table: "UserBooks");

            migrationBuilder.RenameColumn(
                name: "PictureMimeType",
                table: "UserBooks",
                newName: "Folder");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "UserBooks",
                newName: "BookID");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "UserBooks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "OverallRating",
                table: "UserBooks",
                type: "float",
                nullable: true);
        }
    }
}
