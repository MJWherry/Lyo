using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobProductionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "end_date_utc",
                schema: "job",
                table: "job_schedule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "misfire_policy",
                schema: "job",
                table: "job_schedule",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "Skip");

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date_utc",
                schema: "job",
                table: "job_schedule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "time_zone_id",
                schema: "job",
                table: "job_schedule",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "progress_message",
                schema: "job",
                table: "job_run",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "progress_percent",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "retention_days",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "retry_backoff_type",
                schema: "job",
                table: "job_definition",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "Linear");

            migrationBuilder.CreateTable(
                name: "job_worker_instance",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    worker_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    machine_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    process_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    in_flight_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    started_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_heartbeat_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_worker_instance", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_worker_instance_last_heartbeat_utc",
                schema: "job",
                table: "job_worker_instance",
                column: "last_heartbeat_utc");

            migrationBuilder.CreateIndex(
                name: "ix_job_worker_instance_worker_type",
                schema: "job",
                table: "job_worker_instance",
                column: "worker_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_worker_instance",
                schema: "job");

            migrationBuilder.DropColumn(
                name: "end_date_utc",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "misfire_policy",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "start_date_utc",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "time_zone_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "priority",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "progress_message",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "progress_percent",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "priority",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "retention_days",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "retry_backoff_type",
                schema: "job",
                table: "job_definition");
        }
    }
}
