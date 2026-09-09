using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class DropStagedFileUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staged_file_upload");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staged_file_upload",
                columns: table => new
                {
                    stage_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    charset = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    committed_file_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    content_hash = table.Column<byte[]>(type: "BLOB", nullable: true),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    created_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    declared_max_size_bytes = table.Column<long>(type: "INTEGER", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    failure_reason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    hash_algorithm = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    observed_size_bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    original_file_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    owner_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    path_prefix = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    provider_kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    provider_state = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    storage_location = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    tenant_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staged_file_upload", x => x.stage_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staged_file_upload_status_expires",
                table: "staged_file_upload",
                columns: new[] { "status", "expires_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_staged_file_upload_tenant_created",
                table: "staged_file_upload",
                columns: new[] { "tenant_id", "created_utc" });
        }
    }
}
