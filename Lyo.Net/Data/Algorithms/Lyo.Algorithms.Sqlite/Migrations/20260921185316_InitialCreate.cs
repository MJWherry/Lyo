using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Algorithms.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "algorithm_definition",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    current_revision = table.Column<int>(type: "INTEGER", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "algorithm_definition_label",
                columns: table => new
                {
                    definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    revision = table.Column<int>(type: "INTEGER", nullable: false),
                    is_mutable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition_label", x => new { x.definition_id, x.label });
                    table.ForeignKey(
                        name: "FK_algorithm_definition_label_algorithm_definition_definition_id",
                        column: x => x.definition_id,
                        principalTable: "algorithm_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "algorithm_definition_revision",
                columns: table => new
                {
                    definition_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    revision = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "TEXT", nullable: false),
                    payload = table.Column<string>(type: "TEXT", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_algorithm_definition_revision", x => new { x.definition_id, x.revision });
                    table.ForeignKey(
                        name: "FK_algorithm_definition_revision_algorithm_definition_definition_id",
                        column: x => x.definition_id,
                        principalTable: "algorithm_definition",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_algorithm_definition_key",
                table: "algorithm_definition",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "algorithm_definition_label");

            migrationBuilder.DropTable(
                name: "algorithm_definition_revision");

            migrationBuilder.DropTable(
                name: "algorithm_definition");
        }
    }
}
