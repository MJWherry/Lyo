using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobParameterOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "options",
                schema: "job",
                table: "job_parameter",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "options",
                schema: "job",
                table: "job_parameter");
        }
    }
}
