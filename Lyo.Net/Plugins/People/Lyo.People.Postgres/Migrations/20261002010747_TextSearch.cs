using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.People.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "people",
                table: "person",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(NULLIF(preferred_name, ''), btrim(COALESCE(first_name, '') || ' ' || COALESCE(last_name, ''))))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_search_vector",
                schema: "people",
                table: "person",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_person_search_vector",
                schema: "people",
                table: "person");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "people",
                table: "person");
        }
    }
}
