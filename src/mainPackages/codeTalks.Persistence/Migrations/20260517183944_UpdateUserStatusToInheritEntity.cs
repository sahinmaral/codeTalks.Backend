using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserStatusToInheritEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "0c2162d3-f098-4551-9ad5-de1aa3960834");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "aa5b9e21-9c64-426e-a1a2-b6032c484c89");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "UserStatuses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserStatuses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserStatuses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "4aac6dd6-e5c9-47dc-99e9-e8b3f26264c5", "ed1ad7f7-1305-45f4-ba15-97b3ad467869", "Moderator", "MODERATOR" },
                    { "9fc4de86-4793-4709-b83f-0b6988f92f64", "cedce06c-6f78-45a4-8d98-f0dc5838ba90", "User", "USER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserStatuses_UserId",
                table: "UserStatuses",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses");

            migrationBuilder.DropIndex(
                name: "IX_UserStatuses_UserId",
                table: "UserStatuses");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "4aac6dd6-e5c9-47dc-99e9-e8b3f26264c5");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "9fc4de86-4793-4709-b83f-0b6988f92f64");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserStatuses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserStatuses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserStatuses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses",
                column: "UserId");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0c2162d3-f098-4551-9ad5-de1aa3960834", "ba7d9071-9f7e-4635-86ee-ac47ba64c7ee", "User", "USER" },
                    { "aa5b9e21-9c64-426e-a1a2-b6032c484c89", "db585dcc-d123-447f-91a9-57b8ae71fbd8", "Moderator", "MODERATOR" }
                });
        }
    }
}
