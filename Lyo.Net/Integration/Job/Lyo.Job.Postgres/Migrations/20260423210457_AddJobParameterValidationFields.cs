using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobParameterValidationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allowed_values",
                schema: "job",
                table: "job_parameter",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_length",
                schema: "job",
                table: "job_parameter",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_length",
                schema: "job",
                table: "job_parameter",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "validation_regex",
                schema: "job",
                table: "job_parameter",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_values",
                schema: "job",
                table: "job_parameter");

            migrationBuilder.DropColumn(
                name: "max_length",
                schema: "job",
                table: "job_parameter");

            migrationBuilder.DropColumn(
                name: "min_length",
                schema: "job",
                table: "job_parameter");

            migrationBuilder.DropColumn(
                name: "validation_regex",
                schema: "job",
                table: "job_parameter");
        }
    }
}
