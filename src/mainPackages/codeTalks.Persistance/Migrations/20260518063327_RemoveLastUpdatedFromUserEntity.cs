using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace codeTalks.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastUpdatedFromUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "4aac6dd6-e5c9-47dc-99e9-e8b3f26264c5");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "9fc4de86-4793-4709-b83f-0b6988f92f64");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "UserStatuses");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "b1c487ef-1fa4-4f96-a8ab-b6cb14216a86", "f370f307-6959-4896-ae23-0ea826a00261", "Moderator", "MODERATOR" },
                    { "ec128130-96b8-4fa9-b624-a7fd8bf9c5d2", "d3a9a937-e0a2-401c-9cca-b95403e44cf8", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "b1c487ef-1fa4-4f96-a8ab-b6cb14216a86");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "ec128130-96b8-4fa9-b624-a7fd8bf9c5d2");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "UserStatuses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "4aac6dd6-e5c9-47dc-99e9-e8b3f26264c5", "ed1ad7f7-1305-45f4-ba15-97b3ad467869", "Moderator", "MODERATOR" },
                    { "9fc4de86-4793-4709-b83f-0b6988f92f64", "cedce06c-6f78-45a4-8d98-f0dc5838ba90", "User", "USER" }
                });
        }
    }
}
