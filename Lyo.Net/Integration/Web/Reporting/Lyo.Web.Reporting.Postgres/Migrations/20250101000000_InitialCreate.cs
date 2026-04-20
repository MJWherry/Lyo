using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Web.Reporting.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "report");

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "report",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    report_data_json = table.Column<string>(type: "text", nullable: false),
                    parameter_type_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_created_timestamp",
                schema: "report",
                table: "reports",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_reports_is_active",
                schema: "report",
                table: "reports",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_reports_is_active_name",
                schema: "report",
                table: "reports",
                columns: new[] { "is_active", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_updated_timestamp",
                schema: "report",
                table: "reports",
                column: "updated_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_reports_name",
                schema: "report",
                table: "reports",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_reports_parameter_type_name",
                schema: "report",
                table: "reports",
                column: "parameter_type_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "report");
        }
    }
}

