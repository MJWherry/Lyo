using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Podcast.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "podcast");

            migrationBuilder.CreateTable(
                name: "genre",
                schema: "podcast",
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
                        principalSchema: "podcast",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "show",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    overview = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_show", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season", x => x.id);
                    table.ForeignKey(
                        name: "FK_season_show_show_id",
                        column: x => x.show_id,
                        principalSchema: "podcast",
                        principalTable: "show",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "show_genre",
                schema: "podcast",
                columns: table => new
                {
                    show_id = table.Column<Guid>(type: "uuid", nullable: false),
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_show_genre", x => new { x.show_id, x.genre_id });
                    table.ForeignKey(
                        name: "FK_show_genre_genre_genre_id",
                        column: x => x.genre_id,
                        principalSchema: "podcast",
                        principalTable: "genre",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_show_genre_show_show_id",
                        column: x => x.show_id,
                        principalSchema: "podcast",
                        principalTable: "show",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "show_identifier",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_show_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_show_identifier_show_show_id",
                        column: x => x.show_id,
                        principalSchema: "podcast",
                        principalTable: "show",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    runtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    published_date = table.Column<DateOnly>(type: "date", nullable: true),
                    audio_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    description = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    transcript = table.Column<string>(type: "text", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('english', COALESCE(transcript, ''))", stored: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_episode", x => x.id);
                    table.ForeignKey(
                        name: "FK_episode_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "podcast",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_id = table.Column<Guid>(type: "uuid", nullable: true),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_credit_episode_episode_id",
                        column: x => x.episode_id,
                        principalSchema: "podcast",
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_show_show_id",
                        column: x => x.show_id,
                        principalSchema: "podcast",
                        principalTable: "show",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode_identifier",
                schema: "podcast",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_episode_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_episode_identifier_episode_episode_id",
                        column: x => x.episode_id,
                        principalSchema: "podcast",
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_podcast_credit_episode_role_ordinal",
                schema: "podcast",
                table: "credit",
                columns: new[] { "episode_id", "role", "ordinal" },
                unique: true,
                filter: "episode_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_podcast_credit_show_role_ordinal",
                schema: "podcast",
                table: "credit",
                columns: new[] { "show_id", "role", "ordinal" },
                unique: true,
                filter: "show_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_podcast_episode_number",
                schema: "podcast",
                table: "episode",
                columns: new[] { "season_id", "episode_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_podcast_episode_search_vector",
                schema: "podcast",
                table: "episode",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_episode_identifier_episode_id",
                schema: "podcast",
                table: "episode_identifier",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "ix_podcast_episode_identifier_scheme",
                schema: "podcast",
                table: "episode_identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_genre_parent_genre_id",
                schema: "podcast",
                table: "genre",
                column: "parent_genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_podcast_genre_slug",
                schema: "podcast",
                table: "genre",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_podcast_season_number",
                schema: "podcast",
                table: "season",
                columns: new[] { "show_id", "season_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_podcast_show_slug",
                schema: "podcast",
                table: "show",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_show_genre_genre_id",
                schema: "podcast",
                table: "show_genre",
                column: "genre_id");

            migrationBuilder.CreateIndex(
                name: "ix_podcast_show_identifier_scheme",
                schema: "podcast",
                table: "show_identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_show_identifier_show_id",
                schema: "podcast",
                table: "show_identifier",
                column: "show_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "episode_identifier",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "show_genre",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "show_identifier",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "episode",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "genre",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "season",
                schema: "podcast");

            migrationBuilder.DropTable(
                name: "show",
                schema: "podcast");
        }
    }
}
