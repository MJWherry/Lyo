using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Email.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "email");

            migrationBuilder.CreateTable(
                name: "email_logs",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    to_addresses_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    cc_addresses_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    bcc_addresses_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_attachment_logs",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_storage_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metadata_json = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_attachment_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_attachment_logs_email_logs_email_log_id",
                        column: x => x.email_log_id,
                        principalSchema: "email",
                        principalTable: "email_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_attachment_logs_email_log_id",
                schema: "email",
                table: "email_attachment_logs",
                column: "email_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_attachment_logs_file_storage_id",
                schema: "email",
                table: "email_attachment_logs",
                column: "file_storage_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_attachment_logs_template_id",
                schema: "email",
                table: "email_attachment_logs",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_created_timestamp",
                schema: "email",
                table: "email_logs",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_from_address",
                schema: "email",
                table: "email_logs",
                column: "from_address");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_is_success",
                schema: "email",
                table: "email_logs",
                column: "is_success");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_message_id",
                schema: "email",
                table: "email_logs",
                column: "message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_attachment_logs",
                schema: "email");

            migrationBuilder.DropTable(
                name: "email_logs",
                schema: "email");
        }
    }
}
