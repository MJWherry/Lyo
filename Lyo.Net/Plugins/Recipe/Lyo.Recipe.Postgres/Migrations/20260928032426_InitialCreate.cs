using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Recipe.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "recipe");

            migrationBuilder.CreateTable(
                name: "attachment",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_category_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "recipe",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredient",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    default_unit = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    notes = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipe",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    servings = table.Column<int>(type: "integer", nullable: true),
                    prep_minutes = table.Column<int>(type: "integer", nullable: true),
                    cook_minutes = table.Column<int>(type: "integer", nullable: true),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    source_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    yield_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipe_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "recipe",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredient_line",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ingredient_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    unit = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    preparation = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    group_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredient_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_ingredient_line_ingredient_ingredient_id",
                        column: x => x.ingredient_id,
                        principalSchema: "recipe",
                        principalTable: "ingredient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ingredient_line_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "recipe",
                        principalTable: "recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "step",
                schema: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    instruction = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    group_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step", x => x.id);
                    table.ForeignKey(
                        name: "FK_step_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "recipe",
                        principalTable: "recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_attachment_file",
                schema: "recipe",
                table: "attachment",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_attachment_subject",
                schema: "recipe",
                table: "attachment",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_attachment_tenant",
                schema: "recipe",
                table: "attachment",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_category_parent",
                schema: "recipe",
                table: "category",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_category_tenant",
                schema: "recipe",
                table: "category",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_category_slug",
                schema: "recipe",
                table: "category",
                column: "slug",
                unique: true,
                filter: "\"tenant_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_category_slug_tenant",
                schema: "recipe",
                table: "category",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_ingredient_tenant",
                schema: "recipe",
                table: "ingredient",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_ingredient_slug",
                schema: "recipe",
                table: "ingredient",
                column: "slug",
                unique: true,
                filter: "\"tenant_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_ingredient_slug_tenant",
                schema: "recipe",
                table: "ingredient",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_line_ingredient",
                schema: "recipe",
                table: "ingredient_line",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_line_recipe_sort",
                schema: "recipe",
                table: "ingredient_line",
                columns: new[] { "recipe_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_line_tenant",
                schema: "recipe",
                table: "ingredient_line",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_recipe_category",
                schema: "recipe",
                table: "recipe",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_recipe_tenant",
                schema: "recipe",
                table: "recipe",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_recipe_slug",
                schema: "recipe",
                table: "recipe",
                column: "slug",
                unique: true,
                filter: "\"tenant_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_recipe_recipe_slug_tenant",
                schema: "recipe",
                table: "recipe",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_step_recipe_sort",
                schema: "recipe",
                table: "step",
                columns: new[] { "recipe_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_step_tenant",
                schema: "recipe",
                table: "step",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "ingredient_line",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "step",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "ingredient",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "recipe",
                schema: "recipe");

            migrationBuilder.DropTable(
                name: "category",
                schema: "recipe");
        }
    }
}
