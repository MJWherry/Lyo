using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDefinitionParameterKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dedupe existing rows before the unique index: for each (definition, key) group
            // (case-insensitive, matching write-time validation) keep the newest row.
            migrationBuilder.Sql(
                """
                DELETE FROM reporting.report_definition_parameter a
                USING reporting.report_definition_parameter b
                WHERE a.report_definition_id = b.report_definition_id
                  AND lower(a.key) = lower(b.key)
                  AND a.id <> b.id
                  AND (a.created_timestamp, a.id) < (b.created_timestamp, b.id);
                """);

            migrationBuilder.DropIndex(
                name: "ix_report_definition_parameter_definition_key",
                schema: "reporting",
                table: "report_definition_parameter");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_parameter_definition_key",
                schema: "reporting",
                table: "report_definition_parameter",
                columns: new[] { "report_definition_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_report_definition_parameter_definition_key",
                schema: "reporting",
                table: "report_definition_parameter");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_parameter_definition_key",
                schema: "reporting",
                table: "report_definition_parameter",
                columns: new[] { "report_definition_id", "key" });
        }
    }
}
