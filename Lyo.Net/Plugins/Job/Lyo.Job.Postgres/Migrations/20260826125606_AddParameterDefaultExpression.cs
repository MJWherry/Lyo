using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterDefaultExpression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "default_kind",
                schema: "job",
                table: "job_parameter",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Literal");

            migrationBuilder.AddColumn<string>(
                name: "default_template",
                schema: "job",
                table: "job_parameter",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_kind",
                schema: "job",
                table: "job_parameter");

            migrationBuilder.DropColumn(
                name: "default_template",
                schema: "job",
                table: "job_parameter");
        }
    }
}
