using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsShortToVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShort",
                table: "Videos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsShort",
                table: "Videos");
        }
    }
}
