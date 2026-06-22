using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace codeTalks.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailPhotoToUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPhotoURL",
                table: "Channels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailPhotoURL",
                table: "Channels");
        }
    }
}
