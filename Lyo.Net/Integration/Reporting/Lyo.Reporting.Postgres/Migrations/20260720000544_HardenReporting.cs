using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class HardenReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_report_generation_report_definition_report_definition_id",
                schema: "reporting",
                table: "report_generation");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "reporting",
                table: "report_generation",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_timestamp",
                schema: "reporting",
                table: "report_definition",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_file_name",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_format",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_path_prefix",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "generation_profile_key",
                schema: "reporting",
                table: "report_definition",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_definition_generation_profile_key",
                schema: "reporting",
                table: "report_definition",
                column: "generation_profile_key");

            migrationBuilder.AddForeignKey(
                name: "FK_report_generation_report_definition_report_definition_id",
                schema: "reporting",
                table: "report_generation",
                column: "report_definition_id",
                principalSchema: "reporting",
                principalTable: "report_definition",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_report_generation_report_definition_report_definition_id",
                schema: "reporting",
                table: "report_generation");

            migrationBuilder.DropIndex(
                name: "ix_report_definition_generation_profile_key",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "reporting",
                table: "report_generation");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.DropColumn(
                name: "default_file_name",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.DropColumn(
                name: "default_format",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.DropColumn(
                name: "default_path_prefix",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.DropColumn(
                name: "generation_profile_key",
                schema: "reporting",
                table: "report_definition");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_timestamp",
                schema: "reporting",
                table: "report_definition",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_report_generation_report_definition_report_definition_id",
                schema: "reporting",
                table: "report_generation",
                column: "report_definition_id",
                principalSchema: "reporting",
                principalTable: "report_definition",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
