﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace websmtp.Migrations
{
    /// <inheritdoc />
    public partial class MessageAttachementsCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttachementsCount",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachementsCount",
                table: "Messages");
        }
    }
}
