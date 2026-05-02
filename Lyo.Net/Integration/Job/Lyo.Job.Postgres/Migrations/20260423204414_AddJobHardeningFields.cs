using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobHardeningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_heartbeat_utc",
                schema: "job",
                table: "job_run",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "circuit_breaker_reset_minutes",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "circuit_breaker_threshold",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "circuit_breaker_tripped_at",
                schema: "job",
                table: "job_definition",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_concurrent_runs",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "timeout_minutes",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_heartbeat_utc",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "circuit_breaker_reset_minutes",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "circuit_breaker_threshold",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "circuit_breaker_tripped_at",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "max_concurrent_runs",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "timeout_minutes",
                schema: "job",
                table: "job_definition");
        }
    }
}
