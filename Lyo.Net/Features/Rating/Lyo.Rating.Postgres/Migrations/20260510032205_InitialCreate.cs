using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Rating.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rating");

            migrationBuilder.CreateTable(
                name: "rating",
                schema: "rating",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value = table.Column<decimal>(type: "numeric", nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    dislike_count = table.Column<int>(type: "integer", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_rating", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rating_reaction",
                schema: "rating",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaction_type = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_reaction", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rating_created_at",
                schema: "rating",
                table: "rating",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_rating_expires_at",
                schema: "rating",
                table: "rating",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rating_tenant_context",
                schema: "rating",
                table: "rating",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_tenant_for_entity",
                schema: "rating",
                table: "rating",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_tenant_for_from_subject",
                schema: "rating",
                table: "rating",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id", "subject" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_tenant_from_entity",
                schema: "rating",
                table: "rating",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_reaction_for_entity",
                schema: "rating",
                table: "rating_reaction",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_reaction_for_from",
                schema: "rating",
                table: "rating_reaction",
                columns: new[] { "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rating",
                schema: "rating");

            migrationBuilder.DropTable(
                name: "rating_reaction",
                schema: "rating");
        }
    }
}
