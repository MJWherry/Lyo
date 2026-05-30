using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Geolocation.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "geolocation");

            migrationBuilder.CreateTable(
                name: "address",
                schema: "geolocation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    house_number = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_pre_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    street_post_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    street_address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    unit_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sub_locality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    zipcode4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    county = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sub_administrative_area = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    full_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    coordinates = table.Column<NpgsqlPoint>(type: "point", nullable: true),
                    is_deliverable = table.Column<bool>(type: "boolean", nullable: true),
                    is_merged_address = table.Column<bool>(type: "boolean", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: true),
                    property_indicator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    bldg_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    utility_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    unit_count = table.Column<int>(type: "integer", nullable: true),
                    first_reported_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_reported_date = table.Column<DateOnly>(type: "date", nullable: true),
                    public_first_seen_date = table.Column<DateOnly>(type: "date", nullable: true),
                    geocode_confidence = table.Column<double>(type: "double precision", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    source_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locally_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),

                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_address_source",
                schema: "geolocation",
                table: "address",
                columns: new[] { "source_entity_type", "source_entity_id" },
                unique: true,
                filter: "\"source_entity_type\" IS NOT NULL AND \"source_entity_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address",
                schema: "geolocation");
        }
    }
}
