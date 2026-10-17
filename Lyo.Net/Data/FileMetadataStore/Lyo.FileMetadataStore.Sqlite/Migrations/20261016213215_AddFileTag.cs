using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_tag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    file_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tag_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    automatic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_tag_file_metadata_file_id",
                        column: x => x.file_id,
                        principalTable: "file_metadata",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_tag_tag_type",
                table: "file_tag",
                column: "tag_type");

            migrationBuilder.CreateIndex(
                name: "uq_file_tag_file_type_value",
                table: "file_tag",
                columns: new[] { "file_id", "tag_type", "value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_tag");
        }
    }
}
