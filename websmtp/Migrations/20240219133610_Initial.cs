using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace websmtp.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Raw = table.Column<byte[]>(type: "longblob", nullable: false),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    Subject = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    From = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    To = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    TextContent = table.Column<string>(type: "longtext", nullable: true),
                    HtmlContent = table.Column<string>(type: "longtext", nullable: true),
                    Read = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageAttachement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Filename = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    MimeType = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    ContentId = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    MessageId = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachement_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachement_MessageId",
                table: "MessageAttachement",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageAttachement");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
