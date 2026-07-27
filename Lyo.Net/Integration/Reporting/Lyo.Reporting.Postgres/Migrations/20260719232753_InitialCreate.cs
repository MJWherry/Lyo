using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "report_definition",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    report_data_json = table.Column<string>(type: "text", nullable: false),
                    parameter_type_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_generation",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    report_data_json = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: true),
                    output_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    path_prefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_generation", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_generation_report_definition_report_definition_id",
                        column: x => x.report_definition_id,
                        principalSchema: "reporting",
                        principalTable: "report_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_created_timestamp",
                schema: "reporting",
                table: "report_definition",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_is_active",
                schema: "reporting",
                table: "report_definition",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_name",
                schema: "reporting",
                table: "report_definition",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_created_timestamp",
                schema: "reporting",
                table: "report_generation",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_definition_id",
                schema: "reporting",
                table: "report_generation",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_output_file_id",
                schema: "reporting",
                table: "report_generation",
                column: "output_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_status",
                schema: "reporting",
                table: "report_generation",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_generation",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_definition",
                schema: "reporting");
        }
    }
}
