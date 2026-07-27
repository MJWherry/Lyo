using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class WidenJobDefinitionWorkerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "worker_type",
                schema: "job",
                table: "job_definition",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "worker_type",
                schema: "job",
                table: "job_definition",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
