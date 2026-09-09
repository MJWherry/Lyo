using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Config.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class DefinitionRevisionsAndEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "encrypted_default_value",
                schema: "config",
                table: "config_definition",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_encrypted",
                schema: "config",
                table: "config_definition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "encrypted_value",
                schema: "config",
                table: "config_binding_revision",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "config_definition_revision",
                schema: "config",
                columns: table => new
                {
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_value_type = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_encrypted = table.Column<bool>(type: "boolean", nullable: false),
                    default_value_json = table.Column<string>(type: "jsonb", maxLength: 8192, nullable: true),
                    encrypted_default_value = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_definition_revision", x => new { x.definition_id, x.revision });
                    table.ForeignKey(
                        name: "FK_config_definition_revision_config_definition_definition_id",
                        column: x => x.definition_id,
                        principalSchema: "config",
                        principalTable: "config_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "config_definition_revision",
                schema: "config");

            migrationBuilder.DropColumn(
                name: "encrypted_default_value",
                schema: "config",
                table: "config_definition");

            migrationBuilder.DropColumn(
                name: "is_encrypted",
                schema: "config",
                table: "config_definition");

            migrationBuilder.DropColumn(
                name: "encrypted_value",
                schema: "config",
                table: "config_binding_revision");
        }
    }
}
