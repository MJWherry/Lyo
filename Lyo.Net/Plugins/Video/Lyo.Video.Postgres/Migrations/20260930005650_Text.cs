using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Video.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Text : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "transcript",
                schema: "video",
                table: "title",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transcript",
                schema: "video",
                table: "episode",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "video",
                table: "title",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(transcript, ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "video",
                table: "episode",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(transcript, ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_title_search_vector",
                schema: "video",
                table: "title",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_video_episode_search_vector",
                schema: "video",
                table: "episode",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_video_title_search_vector",
                schema: "video",
                table: "title");

            migrationBuilder.DropIndex(
                name: "ix_video_episode_search_vector",
                schema: "video",
                table: "episode");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "video",
                table: "title");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "video",
                table: "episode");

            migrationBuilder.DropColumn(
                name: "transcript",
                schema: "video",
                table: "title");

            migrationBuilder.DropColumn(
                name: "transcript",
                schema: "video",
                table: "episode");
        }
    }
}
