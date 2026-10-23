using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Music.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Discovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "format_id",
                schema: "music",
                table: "release",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "format",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_format", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "genre",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_genre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genre", x => x.id);
                    table.ForeignKey(
                        name: "FK_genre_genre_parent_genre_id",
                        column: x => x.parent_genre_id,
                        principalSchema: "music",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "release_genre",
                schema: "music",
                columns: table => new
                {
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_genre", x => new { x.release_id, x.genre_id });
                    table.ForeignKey(
                        name: "FK_release_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "music",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_release_genre_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "music",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_release_format_id",
                schema: "music",
                table: "release",
                column: "format_id");

            migrationBuilder.CreateIndex(
                name: "ix_music_format_slug",
                schema: "music",
                table: "format",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genre_parent_genre_id",
                schema: "music",
                table: "genre",
                column: "parent_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_music_genre_slug",
                schema: "music",
                table: "genre",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_genre_genre_id",
                schema: "music",
                table: "release_genre",
                column: "genre_id");

            migrationBuilder.AddForeignKey(
                name: "FK_release_format_format_id",
                schema: "music",
                table: "release",
                column: "format_id",
                principalSchema: "music",
                principalTable: "format",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_release_format_format_id",
                schema: "music",
                table: "release");

            migrationBuilder.DropTable(
                name: "format",
                schema: "music");

            migrationBuilder.DropTable(
                name: "release_genre",
                schema: "music");

            migrationBuilder.DropTable(
                name: "genre",
                schema: "music");

            migrationBuilder.DropIndex(
                name: "IX_release_format_id",
                schema: "music",
                table: "release");

            migrationBuilder.DropColumn(
                name: "format_id",
                schema: "music",
                table: "release");
        }
    }
}
