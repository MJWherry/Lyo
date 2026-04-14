using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Audit.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_changes",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type_assembly_full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    old_values_json = table.Column<string>(type: "jsonb", nullable: false),
                    changed_properties_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    actor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_changes_timestamp",
                schema: "audit",
                table: "audit_changes",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_changes_type",
                schema: "audit",
                table: "audit_changes",
                column: "type_assembly_full_name");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type",
                schema: "audit",
                table: "audit_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type_timestamp",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "event_type", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_timestamp",
                schema: "audit",
                table: "audit_events",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_changes",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "audit");
        }
    }
}
