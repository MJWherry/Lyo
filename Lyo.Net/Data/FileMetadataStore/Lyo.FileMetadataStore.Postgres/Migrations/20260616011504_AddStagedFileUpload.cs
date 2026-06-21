using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddStagedFileUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staged_file_upload",
                schema: "filestore",
                columns: table => new
                {
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    storage_location = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    path_prefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    declared_max_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    observed_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    hash_algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    provider_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_state = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    committed_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staged_file_upload", x => x.stage_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staged_file_upload_status_expires",
                schema: "filestore",
                table: "staged_file_upload",
                columns: new[] { "status", "expires_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_staged_file_upload_tenant_created",
                schema: "filestore",
                table: "staged_file_upload",
                columns: new[] { "tenant_id", "created_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staged_file_upload",
                schema: "filestore");
        }
    }
}
