using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Web.Automation.Postgres.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialAutomationPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "automation");

            migrationBuilder.CreateTable(
                name: "automation_plans",
                schema: "automation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    step_count = table.Column<int>(type: "integer", nullable: false),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation_plans", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_automation_plans_name",
                schema: "automation",
                table: "automation_plans",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automation_plans",
                schema: "automation");
        }
    }
}
