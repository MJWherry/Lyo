using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Ledger.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ledger");

            migrationBuilder.CreateTable(
                name: "account",
                schema: "ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journal",
                schema: "ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    memo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    void_of_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_journal_void_of_journal_id",
                        column: x => x.void_of_journal_id,
                        principalSchema: "ledger",
                        principalTable: "journal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_line",
                schema: "ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    memo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_line_account_account_id",
                        column: x => x.account_id,
                        principalSchema: "ledger",
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_journal_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "ledger",
                        principalTable: "journal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ledger_account_tenant",
                schema: "ledger",
                table: "account",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_ledger_account_tenant_code",
                schema: "ledger",
                table: "account",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_void_of_journal_id",
                schema: "ledger",
                table: "journal",
                column: "void_of_journal_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journal_status",
                schema: "ledger",
                table: "journal",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journal_tenant",
                schema: "ledger",
                table: "journal",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journal_line_account",
                schema: "ledger",
                table: "journal_line",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journal_line_client",
                schema: "ledger",
                table: "journal_line",
                column: "client_id",
                filter: "\"client_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_journal_line_journal",
                schema: "ledger",
                table: "journal_line",
                column: "journal_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journal_line",
                schema: "ledger");

            migrationBuilder.DropTable(
                name: "account",
                schema: "ledger");

            migrationBuilder.DropTable(
                name: "journal",
                schema: "ledger");
        }
    }
}
