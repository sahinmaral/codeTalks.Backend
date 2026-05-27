using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace codeTalks.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddBioToUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "06e030fb-a006-4112-bb8b-26f5388c375f");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "83127698-5649-4fb5-8403-efdc97a632cd");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "60e9f9d2-b8ef-4693-aab4-93b6480c62ba", "fe86da36-726f-4e96-9c01-c543bb5caca9", "Moderator", "MODERATOR" },
                    { "84597abd-cf79-4545-bda4-c12e2f445ae7", "b8ca6cab-d0fb-4d33-8cb0-5b3dad6b989d", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "60e9f9d2-b8ef-4693-aab4-93b6480c62ba");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "84597abd-cf79-4545-bda4-c12e2f445ae7");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "06e030fb-a006-4112-bb8b-26f5388c375f", "1c7feefa-8929-42f1-b931-48ff6090af2b", "User", "USER" },
                    { "83127698-5649-4fb5-8403-efdc97a632cd", "689bcc5e-9a0f-40ca-96b2-308f893efd1a", "Moderator", "MODERATOR" }
                });
        }
    }
}
