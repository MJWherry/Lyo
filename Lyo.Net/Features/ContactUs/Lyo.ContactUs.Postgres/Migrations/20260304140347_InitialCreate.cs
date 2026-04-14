using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.ContactUs.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "contact");

            migrationBuilder.CreateTable(
                name: "contact_submissions",
                schema: "contact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    message = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_submissions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_submissions_created_timestamp",
                schema: "contact",
                table: "contact_submissions",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_contact_submissions_email",
                schema: "contact",
                table: "contact_submissions",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_submissions",
                schema: "contact");
        }
    }
}
