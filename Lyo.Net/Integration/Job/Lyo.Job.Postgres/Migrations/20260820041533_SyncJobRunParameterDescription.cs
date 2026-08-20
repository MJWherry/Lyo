using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SyncJobRunParameterDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Live DB was varchar(100) while the EF model has been 3000 since InitialCreate — no prior migration emitted an AlterColumn.
            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "job",
                table: "job_run_parameter",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "job",
                table: "job_run_parameter",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000,
                oldNullable: true);
        }
    }
}
