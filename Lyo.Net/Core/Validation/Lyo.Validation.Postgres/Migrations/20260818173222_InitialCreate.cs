using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Validation.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "validation");

            migrationBuilder.CreateTable(
                name: "schema",
                schema: "validation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_type_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    constraints_json = table.Column<string>(type: "jsonb", nullable: false),
                    messages_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_validation_schema_target_type",
                schema: "validation",
                table: "schema",
                column: "target_type_name");

            migrationBuilder.CreateIndex(
                name: "ux_validation_schema_key",
                schema: "validation",
                table: "schema",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schema",
                schema: "validation");
        }
    }
}
