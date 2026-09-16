using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Video.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Reuse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "collection_id",
                schema: "video",
                table: "title",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collection",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cut",
                schema: "video",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    runtime_seconds = table.Column<int>(type: "integer", nullable: true),
                    video_file_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cut", x => x.id);
                    table.ForeignKey(
                        name: "FK_cut_title_title_id",
                        column: x => x.title_id,
                        principalSchema: "video",
                        principalTable: "title",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_title_collection_id",
                schema: "video",
                table: "title",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_collection_slug",
                schema: "video",
                table: "collection",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_video_cut_primary",
                schema: "video",
                table: "cut",
                column: "title_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.AddForeignKey(
                name: "FK_title_collection_collection_id",
                schema: "video",
                table: "title",
                column: "collection_id",
                principalSchema: "video",
                principalTable: "collection",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_title_collection_collection_id",
                schema: "video",
                table: "title");

            migrationBuilder.DropTable(
                name: "collection",
                schema: "video");

            migrationBuilder.DropTable(
                name: "cut",
                schema: "video");

            migrationBuilder.DropIndex(
                name: "IX_title_collection_id",
                schema: "video",
                table: "title");

            migrationBuilder.DropColumn(
                name: "collection_id",
                schema: "video",
                table: "title");
        }
    }
}
