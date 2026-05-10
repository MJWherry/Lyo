using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Tag.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tag");

            migrationBuilder.CreateTable(
                name: "tag",
                schema: "tag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tag_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "tag"),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
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
                    table.PrimaryKey("PK_tag", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tag_created_at",
                schema: "tag",
                table: "tag",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_tag_expires_at",
                schema: "tag",
                table: "tag",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tag_name",
                schema: "tag",
                table: "tag",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_tag_tag_type",
                schema: "tag",
                table: "tag",
                column: "tag_type");

            migrationBuilder.CreateIndex(
                name: "ix_tag_tenant_context",
                schema: "tag",
                table: "tag",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_tag_tenant_for_entity",
                schema: "tag",
                table: "tag",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_tag_tenant_entity_name_slug_active",
                schema: "tag",
                table: "tag",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "tag_type", "name", "slug" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tag",
                schema: "tag");
        }
    }
}
