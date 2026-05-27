using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YoutubeChannelId = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Handle = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    SubscriberCount = table.Column<long>(type: "bigint", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AddedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    LastCheckedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelCategory",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "int", nullable: false),
                    ChannelsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelCategory", x => new { x.CategoriesId, x.ChannelsId });
                    table.ForeignKey(
                        name: "FK_ChannelCategory_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelCategory_Channels_ChannelsId",
                        column: x => x.ChannelsId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YoutubeVideoId = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublishedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AddedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    MetadataLastRefreshedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    WatchedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ViewCount = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelCategory_ChannelsId",
                table: "ChannelCategory",
                column: "ChannelsId");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_YoutubeChannelId",
                table: "Channels",
                column: "YoutubeChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ChannelId",
                table: "Videos",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_YoutubeVideoId",
                table: "Videos",
                column: "YoutubeVideoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelCategory");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Channels");
        }
    }
}
