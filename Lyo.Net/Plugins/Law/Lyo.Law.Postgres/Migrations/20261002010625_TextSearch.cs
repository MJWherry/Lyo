using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Law.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "law",
                table: "node",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', COALESCE(body, ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_law_node_search_vector",
                schema: "law",
                table: "node",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_law_node_search_vector",
                schema: "law",
                table: "node");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "law",
                table: "node");
        }
    }
}
