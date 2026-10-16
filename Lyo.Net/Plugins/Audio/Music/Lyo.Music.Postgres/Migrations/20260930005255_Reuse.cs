using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Music.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Reuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "recording_id",
                schema: "music",
                table: "track",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recording",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recording", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_track_recording_id",
                schema: "music",
                table: "track",
                column: "recording_id");

            migrationBuilder.AddForeignKey(
                name: "FK_track_recording_recording_id",
                schema: "music",
                table: "track",
                column: "recording_id",
                principalSchema: "music",
                principalTable: "recording",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_track_recording_recording_id",
                schema: "music",
                table: "track");

            migrationBuilder.DropTable(
                name: "recording",
                schema: "music");

            migrationBuilder.DropIndex(
                name: "IX_track_recording_id",
                schema: "music",
                table: "track");

            migrationBuilder.DropColumn(
                name: "recording_id",
                schema: "music",
                table: "track");
        }
    }
}
