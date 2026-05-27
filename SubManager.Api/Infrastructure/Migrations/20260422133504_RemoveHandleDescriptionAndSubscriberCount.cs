using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHandleDescriptionAndSubscriberCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "Handle",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "SubscriberCount",
                table: "Channels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Channels",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Handle",
                table: "Channels",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubscriberCount",
                table: "Channels",
                type: "bigint",
                nullable: true);
        }
    }
}
