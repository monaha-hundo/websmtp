using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace websmtp.Migrations
{
    /// <inheritdoc />
    public partial class MessagesProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bcc",
                table: "Messages",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Cc",
                table: "Messages",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Importance",
                table: "Messages",
                type: "varchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bcc",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Cc",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Messages");
        }
    }
}
