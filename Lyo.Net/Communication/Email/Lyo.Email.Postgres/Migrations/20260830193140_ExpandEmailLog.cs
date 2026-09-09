using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Email.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEmailLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_attachment_logs_file_storage_id",
                schema: "email",
                table: "email_attachment_logs");

            migrationBuilder.DropIndex(
                name: "ix_email_attachment_logs_template_id",
                schema: "email",
                table: "email_attachment_logs");

            migrationBuilder.DropColumn(
                name: "file_storage_id",
                schema: "email",
                table: "email_attachment_logs");

            migrationBuilder.DropColumn(
                name: "template_id",
                schema: "email",
                table: "email_attachment_logs");

            // varchar → jsonb needs USING; empty/null rows become NULL.
            migrationBuilder.Sql("""
                ALTER TABLE email.email_logs
                    ALTER COLUMN to_addresses_json TYPE jsonb USING CASE WHEN to_addresses_json IS NULL OR btrim(to_addresses_json) = '' THEN NULL ELSE to_addresses_json::jsonb END,
                    ALTER COLUMN cc_addresses_json TYPE jsonb USING CASE WHEN cc_addresses_json IS NULL OR btrim(cc_addresses_json) = '' THEN NULL ELSE cc_addresses_json::jsonb END,
                    ALTER COLUMN bcc_addresses_json TYPE jsonb USING CASE WHEN bcc_addresses_json IS NULL OR btrim(bcc_addresses_json) = '' THEN NULL ELSE bcc_addresses_json::jsonb END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "direction",
                schema: "email",
                table: "email_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Outbound");

            migrationBuilder.AddColumn<string>(
                name: "html_file_name",
                schema: "email",
                table: "email_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "html_file_path",
                schema: "email",
                table: "email_logs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_timestamp",
                schema: "email",
                table: "email_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_timestamp",
                schema: "email",
                table: "email_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_body",
                schema: "email",
                table: "email_logs",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE email.email_attachment_logs
                    ALTER COLUMN metadata_json TYPE jsonb USING CASE WHEN metadata_json IS NULL OR btrim(metadata_json) = '' THEN NULL ELSE metadata_json::jsonb END;
                """);

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "email",
                table: "email_attachment_logs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_direction_created_timestamp",
                schema: "email",
                table: "email_logs",
                columns: new[] { "direction", "created_timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_logs_direction_created_timestamp",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "direction",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "html_file_name",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "html_file_path",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "received_timestamp",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "sent_timestamp",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "text_body",
                schema: "email",
                table: "email_logs");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "email",
                table: "email_attachment_logs");

            migrationBuilder.Sql("""
                ALTER TABLE email.email_logs
                    ALTER COLUMN to_addresses_json TYPE character varying(4000) USING to_addresses_json::text,
                    ALTER COLUMN cc_addresses_json TYPE character varying(4000) USING cc_addresses_json::text,
                    ALTER COLUMN bcc_addresses_json TYPE character varying(4000) USING bcc_addresses_json::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE email.email_attachment_logs
                    ALTER COLUMN metadata_json TYPE character varying(2000) USING metadata_json::text;
                """);

            migrationBuilder.AddColumn<string>(
                name: "file_storage_id",
                schema: "email",
                table: "email_attachment_logs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_id",
                schema: "email",
                table: "email_attachment_logs",
                type: "uuid",
                nullable: true);

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
        }
    }
}
