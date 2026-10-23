using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Recipe.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class StepLinksAndUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingredient_line_step",
                schema: "recipe",
                columns: table => new
                {
                    line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient_line_step", x => new { x.line_id, x.step_id });
                    table.ForeignKey(
                        name: "FK_ingredient_line_step_ingredient_line_line_id",
                        column: x => x.line_id,
                        principalSchema: "recipe",
                        principalTable: "ingredient_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ingredient_line_step_step_step_id",
                        column: x => x.step_id,
                        principalSchema: "recipe",
                        principalTable: "step",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_line_step_step",
                schema: "recipe",
                table: "ingredient_line_step",
                column: "step_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_unit_tenant",
                schema: "recipe",
                table: "unit",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_unit_abbr",
                schema: "recipe",
                table: "unit",
                column: "abbreviation",
                unique: true,
                filter: "\"tenant_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_unit_abbr_tenant",
                schema: "recipe",
                table: "unit",
                columns: new[] { "tenant_id", "abbreviation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingredient_line_step",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "unit",
                schema: "recipe");
        }
    }
}
