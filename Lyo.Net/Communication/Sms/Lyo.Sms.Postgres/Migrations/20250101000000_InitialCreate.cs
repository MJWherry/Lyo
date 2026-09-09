using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Sms.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sms");

            migrationBuilder.CreateTable(
                name: "sms_logs", schema: "sms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    media_urls_json = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    elapsed_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_code = table.Column<int>(type: "integer", nullable: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_sent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sms_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_created_at",
                schema: "sms",
                table: "sms_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_from",
                schema: "sms",
                table: "sms_logs",
                column: "from");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_is_success",
                schema: "sms",
                table: "sms_logs",
                column: "is_success");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_is_success_created_at",
                schema: "sms",
                table: "sms_logs",
                columns: new[] { "is_success", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_message_id",
                schema: "sms",
                table: "sms_logs",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_status",
                schema: "sms",
                table: "sms_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_to",
                schema: "sms",
                table: "sms_logs",
                column: "to");

            migrationBuilder.CreateIndex(
                name: "ix_sms_logs_to_created_at",
                schema: "sms",
                table: "sms_logs",
                columns: new[] { "to", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sms_logs",
                schema: "sms");
        }
    }
}

