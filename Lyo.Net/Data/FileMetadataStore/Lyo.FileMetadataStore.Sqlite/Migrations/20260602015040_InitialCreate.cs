using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    file_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    actor_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    data_encryption_key_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    data_encryption_key_version = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_data",
                columns: table => new
                {
                    file_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_data", x => x.file_id);
                });

            migrationBuilder.CreateTable(
                name: "file_download_access_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    file_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    token_hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    created_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    not_before_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    window_start_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    window_end_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    max_downloads = table.Column<int>(type: "INTEGER", nullable: true),
                    download_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    last_consumed_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    is_revoked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    revoked_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_download_access_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    original_file_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    original_file_size = table.Column<long>(type: "INTEGER", nullable: false),
                    original_file_hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    source_file_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    source_file_size = table.Column<long>(type: "INTEGER", nullable: false),
                    source_file_hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    is_compressed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    compression_algorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    compressed_file_size = table.Column<long>(type: "INTEGER", nullable: true),
                    compressed_file_hash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    is_encrypted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    data_encryption_key_algorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    key_encryption_key_algorithm = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    encrypted_file_size = table.Column<long>(type: "INTEGER", nullable: true),
                    encrypted_file_hash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    encrypted_data_encryption_key = table.Column<byte[]>(type: "BLOB", nullable: true),
                    data_encryption_key_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    data_encryption_key_version = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    key_encryption_key_salt = table.Column<byte[]>(type: "BLOB", nullable: true),
                    dek_key_material_bytes = table.Column<byte>(type: "INTEGER", nullable: true),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    path_prefix = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    hash_algorithm = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    availability = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    owner_id = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "multipart_upload_session",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    created_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    target_file_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    path_prefix = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    compress = table.Column<bool>(type: "INTEGER", nullable: false),
                    encrypt = table.Column<bool>(type: "INTEGER", nullable: false),
                    key_id = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    original_file_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    provider_kind = table.Column<int>(type: "INTEGER", nullable: false),
                    provider_state = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                    declared_content_length = table.Column<long>(type: "INTEGER", nullable: true),
                    part_size_bytes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_multipart_upload_session", x => x.session_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_event_type",
                table: "file_audit_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_file_id",
                table: "file_audit_events",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_tenant_timestamp",
                table: "file_audit_events",
                columns: new[] { "tenant_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_file_audit_events_timestamp",
                table: "file_audit_events",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_file_data_file_id",
                table: "file_data",
                column: "file_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_file_download_access_links_expires_at_utc",
                table: "file_download_access_links",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_file_download_access_links_file_id",
                table: "file_download_access_links",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_download_access_links_revoked_expires",
                table: "file_download_access_links",
                columns: new[] { "is_revoked", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_file_download_access_links_tenant_id",
                table: "file_download_access_links",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_download_access_links_token_hash",
                table: "file_download_access_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_original_file_hash",
                table: "file_metadata",
                column: "original_file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_original_file_name",
                table: "file_metadata",
                column: "original_file_name");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_owner_id",
                table: "file_metadata",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_tenant_id",
                table: "file_metadata",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_timestamp",
                table: "file_metadata",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_multipart_upload_session_expires_utc",
                table: "multipart_upload_session",
                column: "expires_utc");

            migrationBuilder.CreateIndex(
                name: "ix_multipart_upload_session_target_file_id",
                table: "multipart_upload_session",
                column: "target_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_audit_events");

            migrationBuilder.DropTable(
                name: "file_data");

            migrationBuilder.DropTable(
                name: "file_download_access_links");

            migrationBuilder.DropTable(
                name: "file_metadata");

            migrationBuilder.DropTable(
                name: "multipart_upload_session");
        }
    }
}
