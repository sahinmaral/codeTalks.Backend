using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsActiveAndDeletedAtOfChannelEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Channels");
        }
    }
}
