using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "filestore");

            migrationBuilder.CreateTable(
                name: "file_metadata",
                schema: "filestore",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_file_size = table.Column<long>(type: "bigint", nullable: false),
                    original_file_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_file_size = table.Column<long>(type: "bigint", nullable: false),
                    source_file_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_compressed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    compression_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    compressed_file_size = table.Column<long>(type: "bigint", nullable: true),
                    compressed_file_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    is_encrypted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    data_encryption_key_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    key_encryption_key_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    encrypted_file_size = table.Column<long>(type: "bigint", nullable: true),
                    encrypted_file_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    encrypted_data_encryption_key = table.Column<byte[]>(type: "bytea", nullable: true),
                    data_encryption_key_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    data_encryption_key_version = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    key_encryption_key_salt = table.Column<byte[]>(type: "bytea", nullable: true),
                    dek_key_material_bytes = table.Column<byte>(type: "smallint", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    path_prefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hash_algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    availability = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_data",
                schema: "filestore",
                columns: table => new
                {
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_data", x => x.file_id);
                });

            migrationBuilder.CreateTable(
                name: "file_audit_events",
                schema: "filestore",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    actor_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    data_encryption_key_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    data_encryption_key_version = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "multipart_upload_session",
                schema: "filestore",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path_prefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    compress = table.Column<bool>(type: "boolean", nullable: false),
                    encrypt = table.Column<bool>(type: "boolean", nullable: false),
                    key_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider_kind = table.Column<int>(type: "integer", nullable: false),
                    provider_state = table.Column<string>(type: "text", nullable: false),
                    declared_content_length = table.Column<long>(type: "bigint", nullable: true),
                    part_size_bytes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_multipart_upload_session", x => x.session_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_original_file_name",
                schema: "filestore",
                table: "file_metadata",
                column: "original_file_name");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_original_file_hash",
                schema: "filestore",
                table: "file_metadata",
                column: "original_file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_timestamp",
                schema: "filestore",
                table: "file_metadata",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_tenant_id",
                schema: "filestore",
                table: "file_metadata",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_data_file_id",
                schema: "filestore",
                table: "file_data",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_event_type",
                schema: "filestore",
                table: "file_audit_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_file_id",
                schema: "filestore",
                table: "file_audit_events",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_tenant_timestamp",
                schema: "filestore",
                table: "file_audit_events",
                columns: new[] { "tenant_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_timestamp",
                schema: "filestore",
                table: "file_audit_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_multipart_upload_session_expires_utc",
                schema: "filestore",
                table: "multipart_upload_session",
                column: "expires_utc");

            migrationBuilder.CreateIndex(
                name: "ix_multipart_upload_session_target_file_id",
                schema: "filestore",
                table: "multipart_upload_session",
                column: "target_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_audit_events",
                schema: "filestore");

            migrationBuilder.DropTable(
                name: "multipart_upload_session",
                schema: "filestore");

            migrationBuilder.DropTable(
                name: "file_data",
                schema: "filestore");

            migrationBuilder.DropTable(
                name: "file_metadata",
                schema: "filestore");
        }
    }
}
