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
                    for_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_values_json = table.Column<string>(type: "jsonb", nullable: false, maxLength: 32768),
                    changed_properties_json = table.Column<string>(type: "jsonb", nullable: false, maxLength: 32768),
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
                    for_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true, maxLength: 8192),
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
                name: "ix_audit_changes_for_entity_timestamp",
                schema: "audit",
                table: "audit_changes",
                columns: new[] { "for_entity_type", "for_entity_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_changes_for_entity_type",
                schema: "audit",
                table: "audit_changes",
                column: "for_entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_changes_from_entity",
                schema: "audit",
                table: "audit_changes",
                columns: new[] { "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_changes_tenant",
                schema: "audit",
                table: "audit_changes",
                column: "tenant_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_for_entity_timestamp",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "for_entity_type", "for_entity_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_from_entity",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_tenant",
                schema: "audit",
                table: "audit_events",
                column: "tenant_id");
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
