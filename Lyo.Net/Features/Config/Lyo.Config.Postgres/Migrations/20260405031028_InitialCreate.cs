using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Config.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.CreateTable(
                name: "config_definition",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_value_type = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "config_binding",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value_type = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_binding", x => x.id);
                    table.ForeignKey(
                        name: "FK_config_binding_config_definition_definition_id",
                        column: x => x.definition_id,
                        principalSchema: "config",
                        principalTable: "config_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_config_binding_entity",
                schema: "config",
                table: "config_binding",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ux_config_binding_definition_entity",
                schema: "config",
                table: "config_binding",
                columns: new[] { "definition_id", "for_entity_type", "for_entity_id" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "config_binding_revision",
                schema: "config",
                columns: table => new
                {
                    binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    value_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_binding_revision", x => new { x.binding_id, x.revision });
                    table.ForeignKey(
                        name: "FK_config_binding_revision_config_binding_binding_id",
                        column: x => x.binding_id,
                        principalSchema: "config",
                        principalTable: "config_binding",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_config_definition_entity_type",
                schema: "config",
                table: "config_definition",
                column: "for_entity_type");

            migrationBuilder.CreateIndex(
                name: "ux_config_definition_entity_type_key",
                schema: "config",
                table: "config_definition",
                columns: new[] { "for_entity_type", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config_binding_revision",
                schema: "config");

            migrationBuilder.DropTable(
                name: "config_binding",
                schema: "config");

            migrationBuilder.DropTable(
                name: "config_definition",
                schema: "config");
        }
    }
}
