using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileSystemWatcher.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "filesystem_watcher");

            migrationBuilder.CreateTable(
                name: "watch",
                schema: "filesystem_watcher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    options_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "snapshot",
                schema: "filesystem_watcher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    taken_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_count = table.Column<int>(type: "integer", nullable: false),
                    directory_count = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    content_hash_algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tree_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_snapshot_watch_watch_id",
                        column: x => x.watch_id,
                        principalSchema: "filesystem_watcher",
                        principalTable: "watch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "change",
                schema: "filesystem_watcher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    change_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_directory = table.Column<bool>(type: "boolean", nullable: false),
                    old_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    new_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    change_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change", x => x.id);
                    table.ForeignKey(
                        name: "FK_change_snapshot_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "filesystem_watcher",
                        principalTable: "snapshot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_change_watch_watch_id",
                        column: x => x.watch_id,
                        principalSchema: "filesystem_watcher",
                        principalTable: "watch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_change_snapshot_id",
                schema: "filesystem_watcher",
                table: "change",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_filesystem_watcher_change_watch_occurred",
                schema: "filesystem_watcher",
                table: "change",
                columns: new[] { "watch_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_filesystem_watcher_snapshot_watch_hash",
                schema: "filesystem_watcher",
                table: "snapshot",
                columns: new[] { "watch_id", "content_hash_algorithm", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_filesystem_watcher_snapshot_watch_taken",
                schema: "filesystem_watcher",
                table: "snapshot",
                columns: new[] { "watch_id", "taken_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_filesystem_watcher_watch_root",
                schema: "filesystem_watcher",
                table: "watch",
                column: "root_path");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change",
                schema: "filesystem_watcher");

            migrationBuilder.DropTable(
                name: "snapshot",
                schema: "filesystem_watcher");

            migrationBuilder.DropTable(
                name: "watch",
                schema: "filesystem_watcher");
        }
    }
}
