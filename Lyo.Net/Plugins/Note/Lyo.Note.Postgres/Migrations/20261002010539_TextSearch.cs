using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Note.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "note",
                table: "note",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(content, ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_note_search_vector",
                schema: "note",
                table: "note",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_note_search_vector",
                schema: "note",
                table: "note");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "note",
                table: "note");
        }
    }
}
