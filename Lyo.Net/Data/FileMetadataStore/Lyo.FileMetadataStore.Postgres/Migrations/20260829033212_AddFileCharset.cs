using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddFileCharset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "charset",
                schema: "filestore",
                table: "staged_file_upload",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "charset",
                schema: "filestore",
                table: "multipart_upload_session",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "charset",
                schema: "filestore",
                table: "file_metadata",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "charset",
                schema: "filestore",
                table: "staged_file_upload");

            migrationBuilder.DropColumn(
                name: "charset",
                schema: "filestore",
                table: "multipart_upload_session");

            migrationBuilder.DropColumn(
                name: "charset",
                schema: "filestore",
                table: "file_metadata");
        }
    }
}
