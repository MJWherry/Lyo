using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Note.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "note");

            migrationBuilder.CreateTable(
                name: "note",
                schema: "note",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_note_for_entity",
                schema: "note",
                table: "note",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_note_from_entity",
                schema: "note",
                table: "note",
                columns: new[] { "from_entity_type", "from_entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note",
                schema: "note");
        }
    }
}
