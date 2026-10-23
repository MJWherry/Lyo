using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Idempotency.Postgres.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "idempotency");

            migrationBuilder.CreateTable(
                name: "entry",
                schema: "idempotency",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    request_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: true),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<byte[]>(type: "bytea", nullable: true),
                    response_content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    response_omitted = table.Column<bool>(type: "boolean", nullable: false),
                    destination = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_entry_live_scope_key",
                schema: "idempotency",
                table: "entry",
                columns: new[] { "scope", "key" },
                unique: true,
                filter: "state IN ('in_progress', 'completed', 'failed')");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_entry_outbox_due",
                schema: "idempotency",
                table: "entry",
                columns: new[] { "kind", "state", "available_at" },
                filter: "kind = 'outbox' AND state = 'in_progress'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entry",
                schema: "idempotency");
        }
    }
}
