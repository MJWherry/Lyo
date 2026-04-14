using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Comment.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "comment");

            migrationBuilder.CreateTable(
                name: "comment",
                schema: "comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    reply_to_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    dislike_count = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comment_for_entity",
                schema: "comment",
                table: "comment",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comment_from_entity",
                schema: "comment",
                table: "comment",
                columns: new[] { "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comment_reply_to",
                schema: "comment",
                table: "comment",
                column: "reply_to_comment_id");

            migrationBuilder.CreateTable(
                name: "comment_reaction",
                schema: "comment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reaction_type = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment_reaction", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_for_entity",
                schema: "comment",
                table: "comment_reaction",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_comment_reaction_for_from",
                schema: "comment",
                table: "comment_reaction",
                columns: new[] { "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment_reaction",
                schema: "comment");

            migrationBuilder.DropTable(
                name: "comment",
                schema: "comment");
        }
    }
}
