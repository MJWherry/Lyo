using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Video.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "video");

            migrationBuilder.CreateTable(
                name: "title",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    runtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    video_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    overview = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_title", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season", x => x.id);
                    table.ForeignKey(
                        name: "FK_season_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "title_identifier",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_title_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_title_identifier_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    runtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    video_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    still_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_episode", x => x.id);
                    table.ForeignKey(
                        name: "FK_episode_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "video",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: true),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    character_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    person_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_episode_episode_id",
                        column: x => x.episode_id,
                        principalSchema: "video",
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode_identifier",
                schema: "video",
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
                        principalSchema: "video",
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "youtube_link",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: true),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: true),
                    video_id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    channel_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    playlist_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_link", x => x.id);
                    table.ForeignKey(
                        name: "FK_youtube_link_episode_episode_id",
                        column: x => x.episode_id,
                        principalSchema: "video",
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_youtube_link_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_video_credit_episode_role_ordinal",
                schema: "video",
                table: "credit",
                columns: new[] { "episode_id", "role", "ordinal" },
                unique: true,
                filter: "episode_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_video_credit_title_role_ordinal",
                schema: "video",
                table: "credit",
                columns: new[] { "title_id", "role", "ordinal" },
                unique: true,
                filter: "title_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_video_episode_number",
                schema: "video",
                table: "episode",
                columns: new[] { "season_id", "episode_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_episode_identifier_episode_id",
                schema: "video",
                table: "episode_identifier",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_episode_identifier_scheme",
                schema: "video",
                table: "episode_identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_season_number",
                schema: "video",
                table: "season",
                columns: new[] { "title_id", "season_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_title_slug",
                schema: "video",
                table: "title",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_title_identifier_title_id",
                schema: "video",
                table: "title_identifier",
                column: "title_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_title_identifier_scheme",
                schema: "video",
                table: "title_identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_youtube_episode",
                schema: "video",
                table: "youtube_link",
                column: "episode_id",
                unique: true,
                filter: "episode_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_video_youtube_title",
                schema: "video",
                table: "youtube_link",
                column: "title_id",
                unique: true,
                filter: "title_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_video_youtube_video_id",
                schema: "video",
                table: "youtube_link",
                column: "video_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit",
                schema: "video");

            migrationBuilder.DropTable(
                name: "episode_identifier",
                schema: "video");

            migrationBuilder.DropTable(
                name: "title_identifier",
                schema: "video");

            migrationBuilder.DropTable(
                name: "youtube_link",
                schema: "video");

            migrationBuilder.DropTable(
                name: "episode",
                schema: "video");

            migrationBuilder.DropTable(
                name: "season",
                schema: "video");

            migrationBuilder.DropTable(
                name: "title",
                schema: "video");
        }
    }
}
