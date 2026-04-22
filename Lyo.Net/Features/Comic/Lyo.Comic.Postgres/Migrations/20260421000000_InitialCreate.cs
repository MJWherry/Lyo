using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Comic.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comic");

            migrationBuilder.CreateTable(
                name: "series",
                schema: "comic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    comic_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    original_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    published_year = table.Column<int>(type: "integer", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alternate_title",
                schema: "comic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alternate_title", x => x.id);
                    table.ForeignKey(
                        name: "FK_alternate_title_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "comic",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "volume",
                schema: "comic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volume_number = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_ref = table.Column<string>(type: "text", nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_volume", x => x.id);
                    table.ForeignKey(
                        name: "FK_volume_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "comic",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chapter",
                schema: "comic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volume_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_number = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source_ref = table.Column<string>(type: "text", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chapter", x => x.id);
                    table.ForeignKey(
                        name: "FK_chapter_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "comic",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chapter_volume_volume_id",
                        column: x => x.volume_id,
                        principalSchema: "comic",
                        principalTable: "volume",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comic_alternate_title_series",
                schema: "comic",
                table: "alternate_title",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_comic_alternate_title_series_title_lang",
                schema: "comic",
                table: "alternate_title",
                columns: new[] { "series_id", "title", "language" });

            migrationBuilder.CreateIndex(
                name: "ix_comic_chapter_series",
                schema: "comic",
                table: "chapter",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_comic_chapter_series_num_lang",
                schema: "comic",
                table: "chapter",
                columns: new[] { "series_id", "chapter_number", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comic_chapter_volume",
                schema: "comic",
                table: "chapter",
                column: "volume_id");

            migrationBuilder.CreateIndex(
                name: "ix_comic_series_slug",
                schema: "comic",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comic_series_status",
                schema: "comic",
                table: "series",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_comic_series_type",
                schema: "comic",
                table: "series",
                column: "comic_type");

            migrationBuilder.CreateIndex(
                name: "ix_comic_volume_series",
                schema: "comic",
                table: "volume",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_comic_volume_series_number",
                schema: "comic",
                table: "volume",
                columns: new[] { "series_id", "volume_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alternate_title",
                schema: "comic");

            migrationBuilder.DropTable(
                name: "chapter",
                schema: "comic");

            migrationBuilder.DropTable(
                name: "volume",
                schema: "comic");

            migrationBuilder.DropTable(
                name: "series",
                schema: "comic");
        }
    }
}
