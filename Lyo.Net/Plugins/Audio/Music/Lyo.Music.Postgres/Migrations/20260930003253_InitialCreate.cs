using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Music.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "music");

            migrationBuilder.CreateTable(
                name: "release_group",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "release",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    release_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    cover_image_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    primary_artist = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release", x => x.id);
                    table.ForeignKey(
                        name: "FK_release_release_group_release_group_id",
                        column: x => x.release_group_id,
                        principalSchema: "music",
                        principalTable: "release_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "identifier",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identifier", x => x.id);
                    table.ForeignKey(
                        name: "FK_identifier_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "music",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "track",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    disc_number = table.Column<int>(type: "integer", nullable: false),
                    track_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    isrc = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    audio_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "music",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit",
                schema: "music",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    track_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                        name: "FK_credit_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "music",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_track_track_id",
                        column: x => x.track_id,
                        principalSchema: "music",
                        principalTable: "track",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_music_credit_release_role_ordinal",
                schema: "music",
                table: "credit",
                columns: new[] { "release_id", "role", "ordinal" },
                unique: true,
                filter: "release_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_music_credit_track_role_ordinal",
                schema: "music",
                table: "credit",
                columns: new[] { "track_id", "role", "ordinal" },
                unique: true,
                filter: "track_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_music_identifier_primary",
                schema: "music",
                table: "identifier",
                column: "release_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_music_identifier_scheme_normalized",
                schema: "music",
                table: "identifier",
                columns: new[] { "scheme", "normalized_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_music_release_primary_artist",
                schema: "music",
                table: "release",
                column: "primary_artist");

            migrationBuilder.CreateIndex(
                name: "ix_music_release_slug",
                schema: "music",
                table: "release",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_release_group_id",
                schema: "music",
                table: "release",
                column: "release_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_music_release_group_slug",
                schema: "music",
                table: "release_group",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_music_track_isrc",
                schema: "music",
                table: "track",
                column: "isrc",
                unique: true,
                filter: "isrc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_music_track_position",
                schema: "music",
                table: "track",
                columns: new[] { "release_id", "disc_number", "track_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit",
                schema: "music");

            migrationBuilder.DropTable(
                name: "identifier",
                schema: "music");

            migrationBuilder.DropTable(
                name: "track",
                schema: "music");

            migrationBuilder.DropTable(
                name: "release",
                schema: "music");

            migrationBuilder.DropTable(
                name: "release_group",
                schema: "music");
        }
    }
}
