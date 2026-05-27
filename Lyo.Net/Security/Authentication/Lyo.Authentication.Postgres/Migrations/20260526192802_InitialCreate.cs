using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Authentication.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "user",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    avatar_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    preferred_language_bcp47 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    scopes_json = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disabled_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disabled_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "linked_identity",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email_at_link = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    scopes_json = table.Column<string>(type: "jsonb", nullable: false),
                    raw_claims_json = table.Column<string>(type: "jsonb", nullable: true),
                    linked_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unlinked_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linked_identity", x => x.id);
                    table.ForeignKey(
                        name: "FK_linked_identity_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "token",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    secret_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ring = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scopes_json = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rotated_from_id = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_token_token_rotated_from_id",
                        column: x => x.rotated_from_id,
                        principalSchema: "user",
                        principalTable: "token",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_token_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_linked_identity_provider",
                schema: "user",
                table: "linked_identity",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "ix_linked_identity_user_id",
                schema: "user",
                table: "linked_identity",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_linked_identity_provider_subject",
                schema: "user",
                table: "linked_identity",
                columns: new[] { "provider", "subject" },
                unique: true,
                filter: "\"unlinked_timestamp\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_token_expires_timestamp",
                schema: "user",
                table: "token",
                column: "expires_timestamp",
                filter: "\"expires_timestamp\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_token_kind_ring",
                schema: "user",
                table: "token",
                columns: new[] { "kind", "ring" });

            migrationBuilder.CreateIndex(
                name: "IX_token_rotated_from_id",
                schema: "user",
                table: "token",
                column: "rotated_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_token_user_id_revoked_timestamp",
                schema: "user",
                table: "token",
                columns: new[] { "user_id", "revoked_timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_user_disabled_timestamp",
                schema: "user",
                table: "user",
                column: "disabled_timestamp",
                filter: "\"disabled_timestamp\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_last_login_timestamp",
                schema: "user",
                table: "user",
                column: "last_login_timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_user_person_id",
                schema: "user",
                table: "user",
                column: "person_id",
                filter: "\"person_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_user_email",
                schema: "user",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateTable(
                name: "event",
                schema: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_event_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "user",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_event_timestamp",
                schema: "user",
                table: "event",
                column: "timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_user_event_kind",
                schema: "user",
                table: "event",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_user_event_user_id",
                schema: "user",
                table: "event",
                column: "user_id",
                filter: "\"user_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event",
                schema: "user");

            migrationBuilder.DropTable(
                name: "linked_identity",
                schema: "user");

            migrationBuilder.DropTable(
                name: "token",
                schema: "user");

            migrationBuilder.DropTable(
                name: "user",
                schema: "user");
        }
    }
}
