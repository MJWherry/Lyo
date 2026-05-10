using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Note.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "note");

            migrationBuilder.CreateTable(
                name: "note",
                schema: "note",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "private")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_note_created_at",
                schema: "note",
                table: "note",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_note_expires_at",
                schema: "note",
                table: "note",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_note_tenant_context",
                schema: "note",
                table: "note",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_note_tenant_for_entity",
                schema: "note",
                table: "note",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_note_tenant_from_entity",
                schema: "note",
                table: "note",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note",
                schema: "note");
        }
    }
}
