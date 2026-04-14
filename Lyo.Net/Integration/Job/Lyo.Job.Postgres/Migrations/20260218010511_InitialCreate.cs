using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.CreateTable(
                name: "job_definition",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    worker_type = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_file_upload",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    upload_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    original_filename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    original_size = table.Column<long>(type: "bigint", nullable: false),
                    original_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    source_directory = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    source_filename = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_size = table.Column<long>(type: "bigint", nullable: false),
                    source_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    encrypted_data_encryption_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    data_encryption_key_version = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_file_upload", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_parallel_restriction",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    base_job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    other_job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_parallel_restriction", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_parallel_restriction_base",
                        column: x => x.base_job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_parallel_restriction_other",
                        column: x => x.other_job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_parameter",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    encrypted_value = table.Column<byte[]>(type: "bytea", nullable: true),
                    allow_multiple = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_parameter", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_parameter_job_definition_job_definition_id",
                        column: x => x.job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_schedule",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    month_flags = table.Column<string>(type: "character varying(108)", maxLength: 108, nullable: false),
                    day_flags = table.Column<string>(type: "character varying(51)", maxLength: 51, nullable: false),
                    times = table.Column<List<string>>(type: "character varying(8)[]", nullable: true),
                    start_time = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    end_time = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    interval_minutes = table.Column<int>(type: "integer", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_schedule", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_schedule_job_definition_job_definition_id",
                        column: x => x.job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_trigger",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggers_job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_job_result_key = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    trigger_comparator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trigger_job_result_value = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
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
                name: "job_schedule_parameter",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_schedule_parameter", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_schedule_parameter_job_schedule_job_schedule_id",
                        column: x => x.job_schedule_id,
                        principalSchema: "job",
                        principalTable: "job_schedule",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_run",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_trigger_id = table.Column<Guid>(type: "uuid", nullable: true),
                    triggered_by_job_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    re_ran_from_job_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    allow_triggers = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_run_job_definition_job_definition_id",
                        column: x => x.job_definition_id,
                        principalSchema: "job",
                        principalTable: "job_definition",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_run_job_schedule_job_schedule_id",
                        column: x => x.job_schedule_id,
                        principalSchema: "job",
                        principalTable: "job_schedule",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_run_job_trigger_job_trigger_id",
                        column: x => x.job_trigger_id,
                        principalSchema: "job",
                        principalTable: "job_trigger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_run_re_ran_from",
                        column: x => x.re_ran_from_job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_job_run_triggered_by",
                        column: x => x.triggered_by_job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_trigger_parameter",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_trigger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "job_run_log",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    context = table.Column<string>(type: "text", nullable: true),
                    stack_trace = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_run_log_job_run_job_run_id",
                        column: x => x.job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_run_parameter",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    encrypted_value = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run_parameter", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_run_parameter_job_run_job_run_id",
                        column: x => x.job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_run_result",
                schema: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    job_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_run_result", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_run_result_job_run_job_run_id",
                        column: x => x.job_run_id,
                        principalSchema: "job",
                        principalTable: "job_run",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_definition_name",
                schema: "job",
                table: "job_definition",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_job_file_upload_original_hash",
                schema: "job",
                table: "job_file_upload",
                column: "original_hash");

            migrationBuilder.CreateIndex(
                name: "ix_job_file_upload_source_hash",
                schema: "job",
                table: "job_file_upload",
                column: "source_hash");

            migrationBuilder.CreateIndex(
                name: "ix_job_parallel_restriction_base_job_definition_id",
                schema: "job",
                table: "job_parallel_restriction",
                column: "base_job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_parallel_restriction_other_job_definition_id",
                schema: "job",
                table: "job_parallel_restriction",
                column: "other_job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_parameter_job_definition_id",
                schema: "job",
                table: "job_parameter",
                column: "job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_parameter_key",
                schema: "job",
                table: "job_parameter",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_job_definition_id",
                schema: "job",
                table: "job_run",
                column: "job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_job_schedule_id",
                schema: "job",
                table: "job_run",
                column: "job_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_job_trigger_id",
                schema: "job",
                table: "job_run",
                column: "job_trigger_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_run_re_ran_from_job_run_id",
                schema: "job",
                table: "job_run",
                column: "re_ran_from_job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_state",
                schema: "job",
                table: "job_run",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_triggered_by_job_run_id",
                schema: "job",
                table: "job_run",
                column: "triggered_by_job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_log_job_run_id",
                schema: "job",
                table: "job_run_log",
                column: "job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_log_level",
                schema: "job",
                table: "job_run_log",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_parameter_job_run_id",
                schema: "job",
                table: "job_run_parameter",
                column: "job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_parameter_key",
                schema: "job",
                table: "job_run_parameter",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_result_job_run_id",
                schema: "job",
                table: "job_run_result",
                column: "job_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_result_key",
                schema: "job",
                table: "job_run_result",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "ix_job_schedule_job_definition_id",
                schema: "job",
                table: "job_schedule",
                column: "job_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_schedule_parameter_job_schedule_id",
                schema: "job",
                table: "job_schedule_parameter",
                column: "job_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_schedule_parameter_key",
                schema: "job",
                table: "job_schedule_parameter",
                column: "key");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_file_upload",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_parallel_restriction",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_parameter",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_run_log",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_run_parameter",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_run_result",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_schedule_parameter",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_trigger_parameter",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_run",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_schedule",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_trigger",
                schema: "job");

            migrationBuilder.DropTable(
                name: "job_definition",
                schema: "job");
        }
    }
}
