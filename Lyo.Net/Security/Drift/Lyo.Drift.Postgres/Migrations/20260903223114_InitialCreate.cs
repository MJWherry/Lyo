using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Drift.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "drift");

            migrationBuilder.CreateTable(
                name: "instance",
                schema: "drift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    machine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    process_id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_heartbeat_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    watches_json = table.Column<string>(type: "jsonb", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "change_event",
                schema: "drift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    watch_root = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    change_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_change_event_instance_instance_id",
                        column: x => x.instance_id,
                        principalSchema: "drift",
                        principalTable: "instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "diff_snapshot",
                schema: "drift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    computed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_changes_json = table.Column<string>(type: "jsonb", nullable: true),
                    system_differences_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diff_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_diff_snapshot_instance_instance_id",
                        column: x => x.instance_id,
                        principalSchema: "drift",
                        principalTable: "instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "structure_snapshot",
                schema: "drift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    watch_root = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    taken_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    content_hash_algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tree_json = table.Column<string>(type: "jsonb", nullable: true),
                    system_info_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_structure_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_structure_snapshot_instance_instance_id",
                        column: x => x.instance_id,
                        principalSchema: "drift",
                        principalTable: "instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_drift_change_instance_root_occurred",
                schema: "drift",
                table: "change_event",
                columns: new[] { "instance_id", "watch_root", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_drift_diff_instance_computed",
                schema: "drift",
                table: "diff_snapshot",
                columns: new[] { "instance_id", "computed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_drift_diff_to_snapshot",
                schema: "drift",
                table: "diff_snapshot",
                column: "to_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ux_drift_instance_key",
                schema: "drift",
                table: "instance",
                column: "instance_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_drift_snapshot_lineage_hash",
                schema: "drift",
                table: "structure_snapshot",
                columns: new[] { "instance_id", "kind", "watch_root", "content_hash_algorithm", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_drift_snapshot_lineage_taken",
                schema: "drift",
                table: "structure_snapshot",
                columns: new[] { "instance_id", "kind", "watch_root", "taken_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_event",
                schema: "drift");

            migrationBuilder.DropTable(
                name: "diff_snapshot",
                schema: "drift");

            migrationBuilder.DropTable(
                name: "structure_snapshot",
                schema: "drift");

            migrationBuilder.DropTable(
                name: "instance",
                schema: "drift");
        }
    }
}
