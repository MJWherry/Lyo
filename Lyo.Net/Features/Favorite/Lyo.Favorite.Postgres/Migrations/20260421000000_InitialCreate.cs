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
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_for_entity",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_favorite_from_entity",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_favorite_for_from_entity",
                schema: "favorite",
                table: "favorite",
                columns: new[] { "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true);
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
