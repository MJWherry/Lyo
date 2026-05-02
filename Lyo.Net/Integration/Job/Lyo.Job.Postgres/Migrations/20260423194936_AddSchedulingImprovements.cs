using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cron_expression",
                schema: "job",
                table: "job_schedule",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_attempt",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_slot_utc",
                schema: "job",
                table: "job_run",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_retry_count",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "retry_backoff_seconds",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_job_run_schedule_slot_unique",
                schema: "job",
                table: "job_run",
                columns: new[] { "job_schedule_id", "scheduled_slot_utc" },
                unique: true,
                filter: "job_schedule_id IS NOT NULL AND scheduled_slot_utc IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_run_schedule_slot_unique",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "cron_expression",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "retry_attempt",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "scheduled_slot_utc",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "max_retry_count",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "retry_backoff_seconds",
                schema: "job",
                table: "job_definition");
        }
    }
}
