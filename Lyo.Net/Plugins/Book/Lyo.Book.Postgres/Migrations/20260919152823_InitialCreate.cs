using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Book.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "book");

            migrationBuilder.CreateTable(
                name: "book_format",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_format", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "book_type",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "genre",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_genre_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genre", x => x.id);
                    table.ForeignKey(
                        name: "FK_genre_genre_parent_genre_id",
                        column: x => x.parent_genre_id,
                        principalSchema: "book",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "series",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    original_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    imprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    planned_book_count = table.Column<int>(type: "integer", nullable: true),
                    started_date = table.Column<DateOnly>(type: "date", nullable: true),
                    ended_date = table.Column<DateOnly>(type: "date", nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    source = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "book",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_index = table.Column<decimal>(type: "numeric", nullable: true),
                    primary_author = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    format_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    original_language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    published_year = table.Column<int>(type: "integer", nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    imprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    edition_statement = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    edition_number = table.Column<int>(type: "integer", nullable: true),
                    print_page_count = table.Column<int>(type: "integer", nullable: true),
                    word_count = table.Column<int>(type: "integer", nullable: true),
                    audience = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    content_rating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    source = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book", x => x.id);
                    table.ForeignKey(
                        name: "FK_book_book_format_format_id",
                        column: x => x.format_id,
                        principalSchema: "book",
                        principalTable: "book_format",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_book_type_type_id",
                        column: x => x.type_id,
                        principalSchema: "book",
                        principalTable: "book_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "book",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "series_alternate_title",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series_alternate_title", x => x.id);
                    table.ForeignKey(
                        name: "FK_series_alternate_title_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "book",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alternate_title",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alternate_title", x => x.id);
                    table.ForeignKey(
                        name: "FK_alternate_title_book_book_id",
                        column: x => x.book_id,
                        principalSchema: "book",
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_genre",
                schema: "book",
                columns: table => new
                {
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_genre", x => new { x.book_id, x.genre_id });
                    table.ForeignKey(
                        name: "FK_book_genre_book_book_id",
                        column: x => x.book_id,
                        principalSchema: "book",
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_book_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "book",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chapter",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_number = table.Column<decimal>(type: "numeric", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    start_print_page = table.Column<int>(type: "integer", nullable: true),
                    end_print_page = table.Column<int>(type: "integer", nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    word_count = table.Column<int>(type: "integer", nullable: true),
                    summary = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    source = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chapter", x => x.id);
                    table.ForeignKey(
                        name: "FK_chapter_book_book_id",
                        column: x => x.book_id,
                        principalSchema: "book",
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contributor",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    person_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contributor", x => x.id);
                    table.ForeignKey(
                        name: "FK_contributor_book_book_id",
                        column: x => x.book_id,
                        principalSchema: "book",
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identifier",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_identifier_book_book_id",
                        column: x => x.book_id,
                        principalSchema: "book",
                        principalTable: "book",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page",
                schema: "book",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    print_page_label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('english', CASE WHEN content_type = 'text/html' THEN regexp_replace(COALESCE(text, ''), '<[^>]+>', ' ', 'g') ELSE COALESCE(text, '') END)", stored: true),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    character_count = table.Column<int>(type: "integer", nullable: false),
                    word_count = table.Column<int>(type: "integer", nullable: false),
                    scan_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page", x => x.id);
                    table.ForeignKey(
                        name: "FK_page_chapter_chapter_id",
                        column: x => x.chapter_id,
                        principalSchema: "book",
                        principalTable: "chapter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_book_alternate_title_primary",
                schema: "book",
                table: "alternate_title",
                column: "book_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_book_book_primary_author",
                schema: "book",
                table: "book",
                column: "primary_author");

            migrationBuilder.CreateIndex(
                name: "ix_book_book_slug",
                schema: "book",
                table: "book",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_book_format_id",
                schema: "book",
                table: "book",
                column: "format_id");

            migrationBuilder.CreateIndex(
                name: "IX_book_series_id",
                schema: "book",
                table: "book",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_book_type_id",
                schema: "book",
                table: "book",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_format_slug",
                schema: "book",
                table: "book_format",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_book_genre_genre_id",
                schema: "book",
                table: "book_genre",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_type_slug",
                schema: "book",
                table: "book_type",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_chapter_book_number_language",
                schema: "book",
                table: "chapter",
                columns: new[] { "book_id", "chapter_number", "language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_contributor_book_role_ordinal",
                schema: "book",
                table: "contributor",
                columns: new[] { "book_id", "role", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_contributor_sort_name",
                schema: "book",
                table: "contributor",
                column: "sort_name");

            migrationBuilder.CreateIndex(
                name: "ix_book_genre_slug",
                schema: "book",
                table: "genre",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genre_parent_genre_id",
                schema: "book",
                table: "genre",
                column: "parent_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_book_identifier_primary",
                schema: "book",
                table: "identifier",
                column: "book_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_book_identifier_scheme_normalized",
                schema: "book",
                table: "identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_page_chapter_number",
                schema: "book",
                table: "page",
                columns: new[] { "chapter_id", "page_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_page_search_vector",
                schema: "book",
                table: "page",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_book_series_slug",
                schema: "book",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_book_series_alternate_title_primary",
                schema: "book",
                table: "series_alternate_title",
                column: "series_id",
                unique: true,
                filter: "is_primary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alternate_title",
                schema: "book");

            migrationBuilder.DropTable(
                name: "book_genre",
                schema: "book");

            migrationBuilder.DropTable(
                name: "contributor",
                schema: "book");

            migrationBuilder.DropTable(
                name: "identifier",
                schema: "book");

            migrationBuilder.DropTable(
                name: "page",
                schema: "book");

            migrationBuilder.DropTable(
                name: "series_alternate_title",
                schema: "book");

            migrationBuilder.DropTable(
                name: "genre",
                schema: "book");

            migrationBuilder.DropTable(
                name: "chapter",
                schema: "book");

            migrationBuilder.DropTable(
                name: "book",
                schema: "book");

            migrationBuilder.DropTable(
                name: "book_format",
                schema: "book");

            migrationBuilder.DropTable(
                name: "book_type",
                schema: "book");

            migrationBuilder.DropTable(
                name: "series",
                schema: "book");
        }
    }
}
