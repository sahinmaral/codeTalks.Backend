using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhoneNumberConfirmedFromUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
