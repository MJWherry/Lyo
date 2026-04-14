using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.ChangeTracker.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "change_tracker");

            migrationBuilder.CreateTable(
                name: "changes",
                schema: "change_tracker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    change_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    old_values_json = table.Column<string>(type: "jsonb", nullable: false),
                    changed_properties_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_changes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_changes_for_entity_timestamp",
                schema: "change_tracker",
                table: "changes",
                columns: new[] { "for_entity_type", "for_entity_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_changes_for_entity_type",
                schema: "change_tracker",
                table: "changes",
                column: "for_entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_changes_from_entity",
                schema: "change_tracker",
                table: "changes",
                columns: new[] { "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_changes_timestamp",
                schema: "change_tracker",
                table: "changes",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "changes",
                schema: "change_tracker");
        }
    }
}
