using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowsAndDropTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_job_run_job_trigger_job_trigger_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropForeignKey(
                name: "fk_job_run_triggered_by",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropTable(
                name: "job_trigger_parameter",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_trigger",
                schema: "job");

            migrationBuilder.DropIndex(
                name: "ix_job_run_job_trigger_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_triggered_by_job_run_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "allow_triggers",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "job_trigger_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "triggered_by_job_run_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "alert_after_consecutive_failures",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "alert_on_failure",
                schema: "job",
                table: "job_definition");

            migrationBuilder.DropColumn(
                name: "alert_webhook_url",
                schema: "job",
                table: "job_definition");

            migrationBuilder.AddColumn<string>(
                name: "condition_json",
                schema: "job",
                table: "job_workflow_step",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "job",
                table: "job_workflow_run",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inputs_json",
                schema: "job",
                table: "job_workflow_run",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_schedule_id",
                schema: "job",
                table: "job_workflow_run",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_slot_utc",
                schema: "job",
                table: "job_workflow_run",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "job_definition_id",
                schema: "job",
                table: "job_schedule",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "job_workflow_id",
                schema: "job",
                table: "job_schedule",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_idempotency_key_unique",
                schema: "job",
                table: "job_workflow_run",
                columns: new[] { "job_workflow_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_job_workflow_run_schedule_slot_unique",
                schema: "job",
                table: "job_workflow_run",
                columns: new[] { "job_schedule_id", "scheduled_slot_utc" },
                unique: true,
                filter: "job_schedule_id IS NOT NULL AND scheduled_slot_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_job_schedule_job_workflow_id",
                schema: "job",
                table: "job_schedule",
                column: "job_workflow_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_job_schedule_exactly_one_owner",
                schema: "job",
                table: "job_schedule",
                sql: "num_nonnulls(job_definition_id, job_workflow_id) = 1");

            migrationBuilder.AddForeignKey(
                name: "fk_job_schedule_job_workflow_job_workflow_id",
                schema: "job",
                table: "job_schedule",
                column: "job_workflow_id",
                principalSchema: "job",
                principalTable: "job_workflow",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_job_workflow_run_job_schedule_job_schedule_id",
                schema: "job",
                table: "job_workflow_run",
                column: "job_schedule_id",
                principalSchema: "job",
                principalTable: "job_schedule",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_job_schedule_job_workflow_job_workflow_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropForeignKey(
                name: "fk_job_workflow_run_job_schedule_job_schedule_id",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropIndex(
                name: "ix_job_workflow_run_idempotency_key_unique",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropIndex(
                name: "ix_job_workflow_run_schedule_slot_unique",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropIndex(
                name: "ix_job_schedule_job_workflow_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropCheckConstraint(
                name: "ck_job_schedule_exactly_one_owner",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.DropColumn(
                name: "condition_json",
                schema: "job",
                table: "job_workflow_step");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropColumn(
                name: "inputs_json",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropColumn(
                name: "job_schedule_id",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropColumn(
                name: "scheduled_slot_utc",
                schema: "job",
                table: "job_workflow_run");

            migrationBuilder.DropColumn(
                name: "job_workflow_id",
                schema: "job",
                table: "job_schedule");

            migrationBuilder.AlterColumn<Guid>(
                name: "job_definition_id",
                schema: "job",
                table: "job_schedule",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_triggers",
                schema: "job",
                table: "job_run",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "job_trigger_id",
                schema: "job",
                table: "job_run",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "triggered_by_job_run_id",
                schema: "job",
                table: "job_run",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "alert_after_consecutive_failures",
                schema: "job",
                table: "job_definition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "alert_on_failure",
                schema: "job",
                table: "job_definition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "alert_webhook_url",
                schema: "job",
                table: "job_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_trigger",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggers_job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    trigger_comparator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trigger_job_result_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    trigger_job_result_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_trigger", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_trigger_job_definition_job_definition_id",
                        column: x => x.job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_trigger_triggers_job_definition",
                        column: x => x.triggers_job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_trigger_parameter",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_trigger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    value = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_trigger_parameter", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_trigger_parameter_job_trigger_job_trigger_id",
                        column: x => x.job_trigger_id,
                        principalSchema: "job",
                        principalTable: "job_trigger",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_run_job_trigger_id",
                schema: "job",
                table: "job_run",
                column: "job_trigger_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_triggered_by_job_run_id",
                schema: "job",
                table: "job_run",
                column: "triggered_by_job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_trigger_job_definition_id",
                schema: "job",
                table: "job_trigger",
                column: "job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_trigger_trigger_job_result_key",
                schema: "job",
                table: "job_trigger",
                column: "trigger_job_result_key");

            migrationBuilder.CreateIndex(
                name: "ix_job_trigger_triggers_job_definition_id",
                schema: "job",
                table: "job_trigger",
                column: "triggers_job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_trigger_parameter_job_trigger_id",
                schema: "job",
                table: "job_trigger_parameter",
                column: "job_trigger_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_trigger_parameter_key",
                schema: "job",
                table: "job_trigger_parameter",
                column: "key");

            migrationBuilder.AddForeignKey(
                name: "fk_job_run_job_trigger_job_trigger_id",
                schema: "job",
                table: "job_run",
                column: "job_trigger_id",
                principalSchema: "job",
                principalTable: "job_trigger",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_job_run_triggered_by",
                schema: "job",
                table: "job_run",
                column: "triggered_by_job_run_id",
                principalSchema: "job",
                principalTable: "job_run",
                principalColumn: "id");
        }
    }
}
