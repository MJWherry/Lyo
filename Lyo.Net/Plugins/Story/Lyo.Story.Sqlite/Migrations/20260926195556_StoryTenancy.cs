using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Story.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class StoryTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "game_key",
                table: "story",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "story",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_story_tenant_game",
                table: "story",
                columns: new[] { "tenant_id", "game_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_story_story_tenant_game",
                table: "story");

            migrationBuilder.DropColumn(
                name: "game_key",
                table: "story");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "story");
        }
    }
}
