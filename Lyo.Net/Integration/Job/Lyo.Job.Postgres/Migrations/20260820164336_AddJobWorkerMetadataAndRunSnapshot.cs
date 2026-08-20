using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWorkerMetadataAndRunSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                schema: "job",
                table: "job_worker_instance",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "worker_instance_id",
                schema: "job",
                table: "job_run",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "worker_machine_name",
                schema: "job",
                table: "job_run",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_process_id",
                schema: "job",
                table: "job_run",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_run_worker_instance_id",
                schema: "job",
                table: "job_run",
                column: "worker_instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_job_run_worker_instance_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                schema: "job",
                table: "job_worker_instance");

            migrationBuilder.DropColumn(
                name: "worker_instance_id",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "worker_machine_name",
                schema: "job",
                table: "job_run");

            migrationBuilder.DropColumn(
                name: "worker_process_id",
                schema: "job",
                table: "job_run");
        }
    }
}
