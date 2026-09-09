using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobRunPerformanceIndexesAndConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The model maps a "Version" shadow property to job_run.xmin as a concurrency token. xmin is a PostgreSQL system column that
            // every table already has, so the scaffolded AddColumn/DropColumn pair was removed by hand: Postgres rejects creating a column
            // with that name ("column name \"xmin\" conflicts with a system column name"). The concurrency token itself needs no DDL. EF
            // reads the model, not this migration, when it emits the optimistic-concurrency predicate.
            migrationBuilder.CreateIndex(
                name: "ix_job_run_definition_created_desc",
                schema: "job",
                table: "job_run",
                columns: new[] { "job_definition_id", "created_timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_job_run_definition_result_created_desc",
                schema: "job",
                table: "job_run",
                columns: new[] { "job_definition_id", "result", "created_timestamp" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_job_run_definition_state",
                schema: "job",
                table: "job_run",
                columns: new[] { "job_definition_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_job_run_finished_timestamp",
                schema: "job",
                table: "job_run",
                column: "finished_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_job_run_state_last_heartbeat",
                schema: "job",
                table: "job_run",
                columns: new[] { "state", "last_heartbeat_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_run_definition_created_desc",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_definition_result_created_desc",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_definition_state",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_finished_timestamp",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropIndex(
                name: "ix_job_run_state_last_heartbeat",
                schema: "job",
                table: "job_run");
        }
    }
}
