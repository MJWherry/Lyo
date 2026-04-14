using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.HomeInventory.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "home_inventory");

            migrationBuilder.CreateTable(
                name: "category",
                schema: "home_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_category_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "home_inventory",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "location",
                schema: "home_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_location", x => x.id);
                    table.ForeignKey(
                        name: "FK_location_location_parent_location_id",
                        column: x => x.parent_location_id,
                        principalSchema: "home_inventory",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item",
                schema: "home_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    owner_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<int>(type: "integer", nullable: false),
                    sku = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    purchase_order_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    sales_order_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    manufacturer_part_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    seller = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    vendor_sku = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    upc = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ean = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    isbn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    model_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    color = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    imei = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ethernet_mac_address = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    wifi_mac_address = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    bluetooth_mac_address = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    msrp = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    weight_grams = table.Column<int>(type: "integer", nullable: true),
                    length_mm = table.Column<int>(type: "integer", nullable: true),
                    width_mm = table.Column<int>(type: "integer", nullable: true),
                    height_mm = table.Column<int>(type: "integer", nullable: true),
                    acquired_date = table.Column<DateTime>(type: "date", nullable: true),
                    warranty_expires = table.Column<DateTime>(type: "date", nullable: true),
                    country_of_origin = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    lot_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    batch_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    custom_attributes_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "home_inventory",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_item_parent_item_id",
                        column: x => x.parent_item_id,
                        principalSchema: "home_inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movement",
                schema: "home_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_by_entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movement", x => x.id);
                    table.ForeignKey(
                        name: "FK_movement_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "home_inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movement_location_from_location_id",
                        column: x => x.from_location_id,
                        principalSchema: "home_inventory",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movement_location_to_location_id",
                        column: x => x.to_location_id,
                        principalSchema: "home_inventory",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                schema: "home_inventory",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_reserved = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reorder_point = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => new { x.item_id, x.location_id });
                    table.ForeignKey(
                        name: "FK_stock_item_item_id",
                        column: x => x.item_id,
                        principalSchema: "home_inventory",
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_location_location_id",
                        column: x => x.location_id,
                        principalSchema: "home_inventory",
                        principalTable: "location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_category_name_parent",
                schema: "home_inventory",
                table: "category",
                columns: new[] { "name", "parent_category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_category_parent",
                schema: "home_inventory",
                table: "category",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_bt_mac",
                schema: "home_inventory",
                table: "item",
                column: "bluetooth_mac_address");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_category",
                schema: "home_inventory",
                table: "item",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_eth_mac",
                schema: "home_inventory",
                table: "item",
                column: "ethernet_mac_address");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_imei",
                schema: "home_inventory",
                table: "item",
                column: "imei");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_owner",
                schema: "home_inventory",
                table: "item",
                columns: new[] { "owner_entity_type", "owner_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_parent",
                schema: "home_inventory",
                table: "item",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_item_serial",
                schema: "home_inventory",
                table: "item",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ux_home_inv_item_sku",
                schema: "home_inventory",
                table: "item",
                column: "sku",
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_location_code",
                schema: "home_inventory",
                table: "location",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_location_parent",
                schema: "home_inventory",
                table: "location",
                column: "parent_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_movement_created",
                schema: "home_inventory",
                table: "movement",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_movement_item",
                schema: "home_inventory",
                table: "movement",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_movement_from_location_id",
                schema: "home_inventory",
                table: "movement",
                column: "from_location_id");

            migrationBuilder.CreateIndex(
                name: "IX_movement_to_location_id",
                schema: "home_inventory",
                table: "movement",
                column: "to_location_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_inv_stock_location",
                schema: "home_inventory",
                table: "stock",
                column: "location_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movement",
                schema: "home_inventory");

            migrationBuilder.DropTable(
                name: "stock",
                schema: "home_inventory");

            migrationBuilder.DropTable(
                name: "item",
                schema: "home_inventory");

            migrationBuilder.DropTable(
                name: "location",
                schema: "home_inventory");

            migrationBuilder.DropTable(
                name: "category",
                schema: "home_inventory");
        }
    }
}
