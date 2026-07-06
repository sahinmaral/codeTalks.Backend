using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerRoleToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "ea552ce1-296c-4607-b7e1-9f0d21de9499", "e633761d-0a88-4b95-bc75-bb19efff08fb", "Owner", "OWNER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "ea552ce1-296c-4607-b7e1-9f0d21de9499");
        }
    }
}
