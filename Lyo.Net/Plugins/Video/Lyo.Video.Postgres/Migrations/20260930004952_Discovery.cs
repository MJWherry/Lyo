using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Video.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Discovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alternate_title",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alternate_title", x => x.id);
                    table.ForeignKey(
                        name: "FK_alternate_title_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "genre",
                schema: "video",
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
                        principalSchema: "video",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "title_genre",
                schema: "video",
                columns: table => new
                {
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_title_genre", x => new { x.title_id, x.genre_id });
                    table.ForeignKey(
                        name: "FK_title_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "video",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_title_genre_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alternate_title_title_id",
                schema: "video",
                table: "alternate_title",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "IX_genre_parent_genre_id",
                schema: "video",
                table: "genre",
                column: "parent_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_genre_slug",
                schema: "video",
                table: "genre",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_title_genre_genre_id",
                schema: "video",
                table: "title_genre",
                column: "genre_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alternate_title",
                schema: "video");

            migrationBuilder.DropTable(
                name: "title_genre",
                schema: "video");

            migrationBuilder.DropTable(
                name: "genre",
                schema: "video");
        }
    }
}
