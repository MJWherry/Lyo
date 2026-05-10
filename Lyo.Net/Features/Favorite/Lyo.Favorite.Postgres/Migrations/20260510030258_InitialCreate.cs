using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Favorite.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "favorite");

            migrationBuilder.CreateTable(
                name: "favorite",
                schema: "favorite",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_favorite", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_created_at",
                schema: "favorite",
                table: "favorite",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_favorite_expires_at",
                schema: "favorite",
                table: "favorite",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_favorite_tenant_context",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_tenant_for_entity",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_tenant_from_entity",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_favorite_tenant_for_from_active",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorite",
                schema: "favorite");
        }
    }
}
