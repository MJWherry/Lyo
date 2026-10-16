using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Audiobook.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audiobook");

            migrationBuilder.CreateTable(
                name: "genre",
                schema: "audiobook",
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
                        principalSchema: "audiobook",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "series",
                schema: "audiobook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work",
                schema: "audiobook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true),
                    runtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    audio_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    overview = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_index = table.Column<decimal>(type: "numeric", nullable: true),
                    transcript = table.Column<string>(type: "text", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('english', COALESCE(transcript, ''))", stored: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "audiobook",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chapter",
                schema: "audiobook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    start_seconds = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    audio_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    transcript = table.Column<string>(type: "text", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('english', COALESCE(transcript, ''))", stored: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chapter", x => x.id);
                    table.ForeignKey(
                        name: "FK_chapter_work_work_id",
                        column: x => x.work_id,
                        principalSchema: "audiobook",
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit",
                schema: "audiobook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    person_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_work_work_id",
                        column: x => x.work_id,
                        principalSchema: "audiobook",
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_genre",
                schema: "audiobook",
                columns: table => new
                {
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_genre", x => new { x.work_id, x.genre_id });
                    table.ForeignKey(
                        name: "FK_work_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "audiobook",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_genre_work_work_id",
                        column: x => x.work_id,
                        principalSchema: "audiobook",
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_identifier",
                schema: "audiobook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_identifier_work_work_id",
                        column: x => x.work_id,
                        principalSchema: "audiobook",
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_chapter_number",
                schema: "audiobook",
                table: "chapter",
                columns: new[] { "work_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_chapter_search_vector",
                schema: "audiobook",
                table: "chapter",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_credit_role_ordinal",
                schema: "audiobook",
                table: "credit",
                columns: new[] { "work_id", "role", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_genre_slug",
                schema: "audiobook",
                table: "genre",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genre_parent_genre_id",
                schema: "audiobook",
                table: "genre",
                column: "parent_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_series_slug",
                schema: "audiobook",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_work_search_vector",
                schema: "audiobook",
                table: "work",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_work_slug",
                schema: "audiobook",
                table: "work",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_series_id",
                schema: "audiobook",
                table: "work",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_genre_genre_id",
                schema: "audiobook",
                table: "work_genre",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_audiobook_identifier_scheme",
                schema: "audiobook",
                table: "work_identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_identifier_work_id",
                schema: "audiobook",
                table: "work_identifier",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chapter",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "credit",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "work_genre",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "work_identifier",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "genre",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "work",
                schema: "audiobook");

            migrationBuilder.DropTable(
                name: "series",
                schema: "audiobook");
        }
    }
}
