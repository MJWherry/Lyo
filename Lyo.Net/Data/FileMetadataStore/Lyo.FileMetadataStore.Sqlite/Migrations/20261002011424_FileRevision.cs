using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class FileRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_revision",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    parent_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    stored_key = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_revision", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_revision_parent_id",
                table: "file_revision",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_revision");
        }
    }
}
