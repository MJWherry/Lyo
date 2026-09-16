using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Algorithms.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "algorithms");

            migrationBuilder.CreateTable(
                name: "algorithm_definition",
                schema: "algorithms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_revision = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "algorithm_definition_label",
                schema: "algorithms",
                columns: table => new
                {
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    is_mutable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition_label", x => new { x.definition_id, x.label });
                    table.ForeignKey(
                        name: "FK_algorithm_definition_label_algorithm_definition_definition_~",
                        column: x => x.definition_id,
                        principalSchema: "algorithms",
                        principalTable: "algorithm_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "algorithm_definition_revision",
                schema: "algorithms",
                columns: table => new
                {
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition_revision", x => new { x.definition_id, x.revision });
                    table.ForeignKey(
                        name: "FK_algorithm_definition_revision_algorithm_definition_definiti~",
                        column: x => x.definition_id,
                        principalSchema: "algorithms",
                        principalTable: "algorithm_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_algorithm_definition_key",
                schema: "algorithms",
                table: "algorithm_definition",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "algorithm_definition_label",
                schema: "algorithms");

            migrationBuilder.DropTable(
                name: "algorithm_definition_revision",
                schema: "algorithms");

            migrationBuilder.DropTable(
                name: "algorithm_definition",
                schema: "algorithms");
        }
    }
}
