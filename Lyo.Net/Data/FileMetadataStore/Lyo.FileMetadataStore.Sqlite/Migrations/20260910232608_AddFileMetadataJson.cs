using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.FileMetadataStore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddFileMetadataJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "multipart_upload_session",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "file_metadata",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "multipart_upload_session");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "file_metadata");
        }
    }
}
