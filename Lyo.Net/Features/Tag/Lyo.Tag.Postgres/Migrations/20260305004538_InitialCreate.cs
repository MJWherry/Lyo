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
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tag_for_entity",
                schema: "tag",
                table: "tag",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tag_for_entity_tag_unique",
                schema: "tag",
                table: "tag",
                columns: new[] { "for_entity_type", "for_entity_id", "tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_tag",
                schema: "tag",
                table: "tag",
                column: "tag");
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
