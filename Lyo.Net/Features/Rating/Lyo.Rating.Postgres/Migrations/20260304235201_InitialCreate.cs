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
                    for_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value = table.Column<decimal>(type: "numeric", nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    dislike_count = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    for_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reaction_type = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_reaction", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rating_for_entity",
                schema: "rating",
                table: "rating",
                columns: new[] { "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_for_from_subject_unique",
                schema: "rating",
                table: "rating",
                columns: new[] { "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rating_from_entity",
                schema: "rating",
                table: "rating",
                columns: new[] { "from_entity_type", "from_entity_id" });

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
                name: "rating_reaction",
                schema: "rating");

            migrationBuilder.DropTable(
                name: "rating",
                schema: "rating");
        }
    }
}
