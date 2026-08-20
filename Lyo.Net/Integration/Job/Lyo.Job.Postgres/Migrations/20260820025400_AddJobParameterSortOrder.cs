using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobParameterSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                schema: "job",
                table: "job_parameter",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE job.job_parameter p
                SET sort_order = s.rn - 1
                FROM (
                  SELECT id, row_number() OVER (PARTITION BY job_definition_id ORDER BY created_timestamp, key) AS rn
                  FROM job.job_parameter
                ) s
                WHERE p.id = s.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_order",
                schema: "job",
                table: "job_parameter");
        }
    }
}
