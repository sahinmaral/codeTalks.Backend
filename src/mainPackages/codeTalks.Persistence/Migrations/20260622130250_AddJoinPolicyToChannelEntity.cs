using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinPolicyToChannelEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JoinPolicy",
                table: "Channels",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinPolicy",
                table: "Channels");
        }
    }
}
