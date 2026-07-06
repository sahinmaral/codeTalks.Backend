using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedInvitationCodeToChannelEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Channels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Channels");
        }
    }
}
