using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace websmtp.Migrations
{
    /// <inheritdoc />
    public partial class SpamRefactor3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DkimFailed",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DmarcFailed",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SpfStatus",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DkimFailed",
                table: "Messages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DmarcFailed",
                table: "Messages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SpfStatus",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
