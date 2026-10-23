using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Story.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class StoryTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "game_key",
                schema: "story",
                table: "story",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "story",
                table: "story",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_story_story_tenant_game",
                schema: "story",
                table: "story",
                columns: new[] { "tenant_id", "game_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_story_story_tenant_game",
                schema: "story",
                table: "story");

            migrationBuilder.DropColumn(
                name: "game_key",
                schema: "story",
                table: "story");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "story",
                table: "story");
        }
    }
}
