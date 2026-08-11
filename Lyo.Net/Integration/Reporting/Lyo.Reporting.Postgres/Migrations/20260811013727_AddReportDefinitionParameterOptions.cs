using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddReportDefinitionParameterOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "options",
                schema: "reporting",
                table: "report_definition_parameter",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "options",
                schema: "reporting",
                table: "report_definition_parameter");
        }
    }
}
