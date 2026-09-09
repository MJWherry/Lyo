using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterDefaultExpression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "default_kind",
                schema: "reporting",
                table: "report_definition_parameter",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Literal");

            migrationBuilder.AddColumn<string>(
                name: "default_template",
                schema: "reporting",
                table: "report_definition_parameter",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_kind",
                schema: "reporting",
                table: "report_definition_parameter");

            migrationBuilder.DropColumn(
                name: "default_template",
                schema: "reporting",
                table: "report_definition_parameter");
        }
    }
}
