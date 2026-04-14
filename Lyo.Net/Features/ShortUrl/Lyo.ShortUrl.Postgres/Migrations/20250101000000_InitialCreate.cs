using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.ShortUrl.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "url");

            migrationBuilder.CreateTable(
                name: "short_urls",
                schema: "url",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    long_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    custom_alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_accessed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    click_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_short_urls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "url_clicks",
                schema: "url",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    short_url_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    referrer = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_url_clicks", x => x.id);
                    table.ForeignKey(
                        name: "FK_url_clicks_short_urls_short_url_id",
                        column: x => x.short_url_id,
                        principalSchema: "url",
                        principalTable: "short_urls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_created_timestamp",
                schema: "url",
                table: "short_urls",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_custom_alias",
                schema: "url",
                table: "short_urls",
                column: "custom_alias",
                unique: true,
                filter: "custom_alias IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_expiration_date",
                schema: "url",
                table: "short_urls",
                column: "expiration_date");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_is_active",
                schema: "url",
                table: "short_urls",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_short_urls_is_active_expiration_date",
                schema: "url",
                table: "short_urls",
                columns: new[] { "is_active", "expiration_date" });

            migrationBuilder.CreateIndex(
                name: "ix_url_clicks_clicked_at",
                schema: "url",
                table: "url_clicks",
                column: "clicked_at");

            migrationBuilder.CreateIndex(
                name: "ix_url_clicks_short_url_id",
                schema: "url",
                table: "url_clicks",
                column: "short_url_id");

            migrationBuilder.CreateIndex(
                name: "ix_url_clicks_short_url_id_clicked_at",
                schema: "url",
                table: "url_clicks",
                columns: new[] { "short_url_id", "clicked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "url_clicks",
                schema: "url");

            migrationBuilder.DropTable(
                name: "short_urls",
                schema: "url");
        }
    }
}

