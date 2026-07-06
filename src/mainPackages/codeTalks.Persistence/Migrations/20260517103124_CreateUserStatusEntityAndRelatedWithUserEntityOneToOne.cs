using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserStatusEntityAndRelatedWithUserEntityOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "60e9f9d2-b8ef-4693-aab4-93b6480c62ba");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "84597abd-cf79-4545-bda4-c12e2f445ae7");

            migrationBuilder.CreateTable(
                name: "UserStatuses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatuses", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStatuses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0c2162d3-f098-4551-9ad5-de1aa3960834", "ba7d9071-9f7e-4635-86ee-ac47ba64c7ee", "User", "USER" },
                    { "aa5b9e21-9c64-426e-a1a2-b6032c484c89", "db585dcc-d123-447f-91a9-57b8ae71fbd8", "Moderator", "MODERATOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserStatuses");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "0c2162d3-f098-4551-9ad5-de1aa3960834");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "aa5b9e21-9c64-426e-a1a2-b6032c484c89");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "60e9f9d2-b8ef-4693-aab4-93b6480c62ba", "fe86da36-726f-4e96-9c01-c543bb5caca9", "Moderator", "MODERATOR" },
                    { "84597abd-cf79-4545-bda4-c12e2f445ae7", "b8ca6cab-d0fb-4d33-8cb0-5b3dad6b989d", "User", "USER" }
                });
        }
    }
}
