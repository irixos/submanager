using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmExistingUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [EmailConfirmed] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
