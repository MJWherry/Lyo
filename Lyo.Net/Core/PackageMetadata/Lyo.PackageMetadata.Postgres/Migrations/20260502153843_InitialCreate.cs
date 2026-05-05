using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.PackageMetadata.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "package_metadata");

            migrationBuilder.CreateTable(
                name: "package",
                schema: "package_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ecosystem = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    artifact_digest_algorithm = table.Column<int>(type: "integer", nullable: false),
                    artifact_digest_hex = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", maxLength: 4000, nullable: true),
                    authors_json = table.Column<string>(type: "text", maxLength: 4096, nullable: true),
                    package_types_json = table.Column<string>(type: "text", maxLength: 4096, nullable: true),
                    tags_json = table.Column<string>(type: "text", maxLength: 4096, nullable: true),
                    project_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    repository_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    license_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    license_expression = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license_expression_syntax_json = table.Column<string>(type: "text", maxLength: 16384, nullable: true),
                    package_details_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stack_prefix",
                schema: "package_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_metadata_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_prefix = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stack_prefix", x => x.id);
                    table.ForeignKey(
                        name: "FK_stack_prefix_package_package_metadata_id",
                        column: x => x.package_metadata_id,
                        principalSchema: "package_metadata",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_package_name",
                schema: "package_metadata",
                table: "package",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_package_name_version",
                schema: "package_metadata",
                table: "package",
                columns: new[] { "name", "version" });

            migrationBuilder.CreateIndex(
                name: "ix_stack_prefix_normalized_prefix_unique",
                schema: "package_metadata",
                table: "stack_prefix",
                column: "normalized_prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stack_prefix_package_metadata_id",
                schema: "package_metadata",
                table: "stack_prefix",
                column: "package_metadata_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stack_prefix",
                schema: "package_metadata");

            migrationBuilder.DropTable(
                name: "package",
                schema: "package_metadata");
        }
    }
}
