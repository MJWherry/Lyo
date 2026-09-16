using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Law.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "law");

            migrationBuilder.CreateTable(
                name: "jurisdiction",
                schema: "law",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jurisdiction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "code",
                schema: "law",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    citation_prefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_host = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_code", x => x.id);
                    table.ForeignKey(
                        name: "FK_code_jurisdiction_jurisdiction_id",
                        column: x => x.jurisdiction_id,
                        principalSchema: "law",
                        principalTable: "jurisdiction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "node",
                schema: "law",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    heading = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    citation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_repealed = table.Column<bool>(type: "boolean", nullable: false),
                    content_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node", x => x.id);
                    table.ForeignKey(
                        name: "FK_node_code_code_id",
                        column: x => x.code_id,
                        principalSchema: "law",
                        principalTable: "code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_node_node_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "law",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "annotation",
                schema: "law",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    related_citation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    related_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annotation", x => x.id);
                    table.ForeignKey(
                        name: "FK_annotation_node_node_id",
                        column: x => x.node_id,
                        principalSchema: "law",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_annotation_node_related_node_id",
                        column: x => x.related_node_id,
                        principalSchema: "law",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "revision",
                schema: "law",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    heading = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    content_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    annotations_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revision", x => x.id);
                    table.ForeignKey(
                        name: "FK_revision_node_node_id",
                        column: x => x.node_id,
                        principalSchema: "law",
                        principalTable: "node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_annotation_related_node_id",
                schema: "law",
                table: "annotation",
                column: "related_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_law_annotation_node_ordinal",
                schema: "law",
                table: "annotation",
                columns: new[] { "node_id", "ordinal" });

            migrationBuilder.CreateIndex(
                name: "ux_law_code_key",
                schema: "law",
                table: "code",
                columns: new[] { "jurisdiction_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_law_jurisdiction_key",
                schema: "law",
                table: "jurisdiction",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_law_node_parent_sort",
                schema: "law",
                table: "node",
                columns: new[] { "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_law_node_citation",
                schema: "law",
                table: "node",
                columns: new[] { "code_id", "citation" },
                unique: true,
                filter: "\"citation\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_law_node_source_key",
                schema: "law",
                table: "node",
                columns: new[] { "code_id", "source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_law_revision_node",
                schema: "law",
                table: "revision",
                columns: new[] { "node_id", "revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotation",
                schema: "law");

            migrationBuilder.DropTable(
                name: "revision",
                schema: "law");

            migrationBuilder.DropTable(
                name: "node",
                schema: "law");

            migrationBuilder.DropTable(
                name: "code",
                schema: "law");

            migrationBuilder.DropTable(
                name: "jurisdiction",
                schema: "law");
        }
    }
}
