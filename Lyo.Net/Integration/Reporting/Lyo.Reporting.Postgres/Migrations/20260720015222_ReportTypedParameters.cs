using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ReportTypedParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "parameters_json",
                schema: "reporting",
                table: "report_generation");

            migrationBuilder.DropColumn(
                name: "parameter_type_name",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.CreateTable(
                name: "report_definition_parameter",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    encrypted_value = table.Column<byte[]>(type: "bytea", nullable: true),
                    allow_multiple = table.Column<bool>(type: "boolean", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    validation_regex = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    min_length = table.Column<int>(type: "integer", nullable: true),
                    max_length = table.Column<int>(type: "integer", nullable: true),
                    allowed_values = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_definition_parameter", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_definition_parameter_report_definition_report_defini~",
                        column: x => x.report_definition_id,
                        principalSchema: "reporting",
                        principalTable: "report_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_generation_parameter",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_generation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    value = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    encrypted_value = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_generation_parameter", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_generation_parameter_report_generation_report_genera~",
                        column: x => x.report_generation_id,
                        principalSchema: "reporting",
                        principalTable: "report_generation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_parameter_definition_id",
                schema: "reporting",
                table: "report_definition_parameter",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_parameter_definition_key",
                schema: "reporting",
                table: "report_definition_parameter",
                columns: new[] { "report_definition_id", "key" });

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_parameter_generation_id",
                schema: "reporting",
                table: "report_generation_parameter",
                column: "report_generation_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_parameter_generation_key",
                schema: "reporting",
                table: "report_generation_parameter",
                columns: new[] { "report_generation_id", "key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_definition_parameter",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "report_generation_parameter",
                schema: "reporting");

            migrationBuilder.AddColumn<string>(
                name: "parameters_json",
                schema: "reporting",
                table: "report_generation",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parameter_type_name",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
