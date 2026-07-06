using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhoneNumberFromUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "98f1a25d-e613-47db-988d-28017919bec4");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "a5bcd8f5-123d-486e-afd2-03825c4a945b");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "98f1a25d-e613-47db-988d-28017919bec4", "38fc531b-a96b-462c-9ae6-c798498dd9f6", "User", "USER" },
                    { "a5bcd8f5-123d-486e-afd2-03825c4a945b", "e7a25c5d-0e93-4b3b-b542-48c12c07fd5a", "Moderator", "MODERATOR" }
                });
        }
    }
}
