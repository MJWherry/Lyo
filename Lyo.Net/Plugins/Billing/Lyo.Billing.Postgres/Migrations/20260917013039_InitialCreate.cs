using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Billing.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "address_payload",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    line2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address_payload", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "biller_profile",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    remittance = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    encrypted_remittance = table.Column<byte[]>(type: "bytea", nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    encrypted_bank_account_number = table.Column<byte[]>(type: "bytea", nullable: true),
                    bank_account_last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biller_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    tax_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    tax_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    encrypted_tax_id = table.Column<byte[]>(type: "bytea", nullable: true),
                    tax_id_last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    invoice_prefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    default_invoice_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    line1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    line2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_address",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_address", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_sequence",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_sequence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    given_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    family_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phone_number",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_number", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "address",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    contact_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    for_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "private")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_address_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contact",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_billing_contact = table.Column<bool>(type: "boolean", nullable: false),
                    for_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "private")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact", x => x.id);
                    table.ForeignKey(
                        name: "FK_contact_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    contact_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    for_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "private")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issue_journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bill_to_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bill_to_contact_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bill_to_line1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bill_to_line2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    bill_to_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    bill_to_region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    bill_to_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    bill_to_country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    encrypted_tax_id = table.Column<byte[]>(type: "bytea", nullable: true),
                    tax_id_last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "phone",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    contact_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    for_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    for_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    from_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    from_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "private")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone", x => x.id);
                    table.ForeignKey(
                        name: "FK_phone_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "charge",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_by_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_charge", x => x.id);
                    table.ForeignKey(
                        name: "FK_charge_client_client_id",
                        column: x => x.client_id,
                        principalSchema: "billing",
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_charge_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    pricing = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_line_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "billing",
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_client",
                schema: "billing",
                table: "address",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_created_at",
                schema: "billing",
                table: "address",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_expires_at",
                schema: "billing",
                table: "address",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_tenant_context",
                schema: "billing",
                table: "address",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_tenant_for_entity",
                schema: "billing",
                table: "address",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_address_tenant_from_entity",
                schema: "billing",
                table: "address",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_billing_address_tenant_for_from_active",
                schema: "billing",
                table: "address",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_billing_biller_profile_tenant",
                schema: "billing",
                table: "biller_profile",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_charge_client",
                schema: "billing",
                table: "charge",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_charge_invoice",
                schema: "billing",
                table: "charge",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_charge_tenant",
                schema: "billing",
                table: "charge",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_client_tax_id_last4",
                schema: "billing",
                table: "client",
                column: "tax_id_last4",
                filter: "\"tax_id_last4\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_client_tenant",
                schema: "billing",
                table: "client",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_client",
                schema: "billing",
                table: "contact",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_created_at",
                schema: "billing",
                table: "contact",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_expires_at",
                schema: "billing",
                table: "contact",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_tenant_context",
                schema: "billing",
                table: "contact",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_tenant_for_entity",
                schema: "billing",
                table: "contact",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_contact_tenant_from_entity",
                schema: "billing",
                table: "contact",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_billing_contact_tenant_for_from_active",
                schema: "billing",
                table: "contact",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_client",
                schema: "billing",
                table: "email",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_created_at",
                schema: "billing",
                table: "email",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_expires_at",
                schema: "billing",
                table: "email",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_tenant_context",
                schema: "billing",
                table: "email",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_tenant_for_entity",
                schema: "billing",
                table: "email",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_email_tenant_from_entity",
                schema: "billing",
                table: "email",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_billing_email_tenant_for_from_active",
                schema: "billing",
                table: "email",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_invoice_client",
                schema: "billing",
                table: "invoice",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_invoice_status",
                schema: "billing",
                table: "invoice",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_billing_invoice_tenant_number",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "number" },
                unique: true,
                filter: "\"number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_invoice_line_invoice",
                schema: "billing",
                table: "invoice_line",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "uq_billing_invoice_sequence_tenant_year",
                schema: "billing",
                table: "invoice_sequence",
                columns: new[] { "tenant_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_payment_invoice",
                schema: "billing",
                table: "payment",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_person_tenant",
                schema: "billing",
                table: "person",
                column: "tenant_id",
                filter: "\"tenant_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_client",
                schema: "billing",
                table: "phone",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_created_at",
                schema: "billing",
                table: "phone",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_expires_at",
                schema: "billing",
                table: "phone",
                column: "expires_at",
                filter: "\"expires_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_tenant_context",
                schema: "billing",
                table: "phone",
                columns: new[] { "tenant_id", "context" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_tenant_for_entity",
                schema: "billing",
                table: "phone",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_phone_tenant_from_entity",
                schema: "billing",
                table: "phone",
                columns: new[] { "tenant_id", "from_entity_type", "from_entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_billing_phone_tenant_for_from_active",
                schema: "billing",
                table: "phone",
                columns: new[] { "tenant_id", "for_entity_type", "for_entity_id", "from_entity_type", "from_entity_id" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "address_payload",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "biller_profile",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "charge",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "contact",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "email",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "email_address",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_line",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_sequence",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "person",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "phone",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "phone_number",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "client",
                schema: "billing");
        }
    }
}
