using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Sms.Twilio.Postgres.Migrations
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
                name: "twilio_sms_logs",
                schema: "sms",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    to = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    media_urls_json = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    elapsed_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_code = table.Column<int>(type: "integer", nullable: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_sent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    num_segments = table.Column<int>(type: "integer", nullable: true),
                    account_sid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    price_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Outbound")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_twilio_sms_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_account_sid",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "account_sid");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_created_timestamp",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_from",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "from");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_is_success",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "is_success");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_is_success_created_timestamp",
                schema: "sms",
                table: "twilio_sms_logs",
                columns: new[] { "is_success", "created_timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_num_segments",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "num_segments");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_status",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_to",
                schema: "sms",
                table: "twilio_sms_logs",
                column: "to");

            migrationBuilder.CreateIndex(
                name: "ix_twilio_sms_logs_to_created_timestamp",
                schema: "sms",
                table: "twilio_sms_logs",
                columns: new[] { "to", "created_timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "twilio_sms_logs",
                schema: "sms");
        }
    }
}

