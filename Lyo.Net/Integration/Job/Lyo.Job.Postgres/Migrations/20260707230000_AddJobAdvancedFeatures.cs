using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAdvancedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "alert_on_failure",
                schema: "job",
                table: "job_definition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "alert_after_consecutive_failures",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "alert_webhook_url",
                schema: "job",
                table: "job_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "definition_version",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "expected_duration_minutes",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_runs_per_hour",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "must_start_by_minutes",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "batch_index",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "batch_total",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "definition_audit_version",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "dry_run",
                schema: "job",
                table: "job_run",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "job",
                table: "job_run",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_job_run_id",
                schema: "job",
                table: "job_run",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sla_breached",
                schema: "job",
                table: "job_run",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                schema: "job",
                table: "job_run",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_calendar",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_calendar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_workflow",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_workflow", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_calendar_window",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    day_flags = table.Column<string>(type: "character varying(51)", maxLength: 51, nullable: false),
                    start_time = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    end_time = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    policy = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "Skip"),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_calendar_window", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_calendar_window_job_calendar_job_calendar_id",
                        column: x => x.job_calendar_id,
                        principalSchema: "job",
                        principalTable: "job_calendar",
                        principalColumn: "id");
                });

            migrationBuilder.AddColumn<Guid>(
                name: "job_calendar_id",
                schema: "job",
                table: "job_schedule",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_workflow_step",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    step_order = table.Column<int>(type: "integer", nullable: false),
                    depends_on_step_ids = table.Column<string>(type: "text", nullable: true),
                    failure_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Stop"),
                    parameters_json = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_workflow_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_workflow_step_job_definition_job_definition_id",
                        column: x => x.job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_workflow_step_job_workflow_job_workflow_id",
                        column: x => x.job_workflow_id,
                        principalSchema: "job",
                        principalTable: "job_workflow",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_workflow_run",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_workflow_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_workflow_run_job_workflow_job_workflow_id",
                        column: x => x.job_workflow_id,
                        principalSchema: "job",
                        principalTable: "job_workflow",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_workflow_run_step",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_workflow_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_workflow_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_workflow_run_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_workflow_run_step_job_run_job_run_id",
                        column: x => x.job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_workflow_run_step_job_workflow_run_job_workflow_run_id",
                        column: x => x.job_workflow_run_id,
                        principalSchema: "job",
                        principalTable: "job_workflow_run",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_workflow_run_step_job_workflow_step_job_workflow_step_id",
                        column: x => x.job_workflow_step_id,
                        principalSchema: "job",
                        principalTable: "job_workflow_step",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_calendar_name",
                schema: "job",
                table: "job_calendar",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_job_calendar_window_job_calendar_id",
                schema: "job",
                table: "job_calendar_window",
                column: "job_calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_schedule_job_calendar_id",
                schema: "job",
                table: "job_schedule",
                column: "job_calendar_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_name",
                schema: "job",
                table: "job_workflow",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_job_workflow_id",
                schema: "job",
                table: "job_workflow_run",
                column: "job_workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_state",
                schema: "job",
                table: "job_workflow_run",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_step_job_run_id",
                schema: "job",
                table: "job_workflow_run_step",
                column: "job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_step_job_workflow_run_id",
                schema: "job",
                table: "job_workflow_run_step",
                column: "job_workflow_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_step_job_workflow_step_id",
                schema: "job",
                table: "job_workflow_run_step",
                column: "job_workflow_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_step_job_definition_id",
                schema: "job",
                table: "job_workflow_step",
                column: "job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_step_job_workflow_id",
                schema: "job",
                table: "job_workflow_step",
                column: "job_workflow_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_idempotency_key_unique",
                schema: "job",
                table: "job_run",
                columns: new[] { "job_definition_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_parent_job_run_id",
                schema: "job",
                table: "job_run",
                column: "parent_job_run_id");

            migrationBuilder.AddForeignKey(
                name: "fk_job_run_parent",
                schema: "job",
                table: "job_run",
                column: "parent_job_run_id",
                principalSchema: "job",
                principalTable: "job_run",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_job_schedule_job_calendar_job_calendar_id",
                schema: "job",
                table: "job_schedule",
                column: "job_calendar_id",
                principalSchema: "job",
                principalTable: "job_calendar",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_job_run_parent",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropForeignKey(
                name: "fk_job_schedule_job_calendar_job_calendar_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropTable(
                name: "job_calendar_window",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_workflow_run_step",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_workflow_run",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_workflow_step",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_workflow",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_calendar",
                schema: "job");

            migrationBuilder.DropIndex(
                name: "ix_job_run_idempotency_key_unique",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_parent_job_run_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_schedule_job_calendar_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "alert_on_failure",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "alert_after_consecutive_failures",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "alert_webhook_url",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "definition_version",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "expected_duration_minutes",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "max_runs_per_hour",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "must_start_by_minutes",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "batch_index",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "batch_total",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "definition_audit_version",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "dry_run",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "parent_job_run_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "sla_breached",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "trace_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "job_calendar_id",
                schema: "job",
                table: "job_schedule");
        }
    }
}
