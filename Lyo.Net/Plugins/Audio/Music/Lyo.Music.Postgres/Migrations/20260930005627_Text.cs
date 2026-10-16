using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Music.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Text : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "lyrics",
                schema: "music",
                table: "track",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "music",
                table: "track",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(lyrics, ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_music_track_search_vector",
                schema: "music",
                table: "track",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_music_track_search_vector",
                schema: "music",
                table: "track");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "music",
                table: "track");

            migrationBuilder.DropColumn(
                name: "lyrics",
                schema: "music",
                table: "track");
        }
    }
}
