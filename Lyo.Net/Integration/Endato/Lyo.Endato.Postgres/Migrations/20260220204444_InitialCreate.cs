using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.Endato.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS endato;");
            migrationBuilder.CreateTable(
                name: "endato_ce_person",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    last_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ce_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "endato_ps_query",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    total_request_execution_time = table.Column<int>(type: "integer", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_time = table.Column<int>(type: "integer", nullable: false),
                    request_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ps_query", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "endato_ce_address",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_ce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    street = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: false),
                    unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    city = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    first_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reported_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ce_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ce_address_endato_ce_person_endato_ce_person_id",
                        column: x => x.endato_ce_person_id,
                        principalTable: "endato_ce_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ce_email_address",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_ce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    is_business = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ce_email_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ce_email_address_endato_ce_person_endato_ce_person_id",
                        column: x => x.endato_ce_person_id,
                        principalTable: "endato_ce_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ce_phone_number",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_ce_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false),
                    first_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reported_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ce_phone_number", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ce_phone_number_endato_ce_person_endato_ce_person_id",
                        column: x => x.endato_ce_person_id,
                        principalTable: "endato_ce_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ce_query",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_line_one = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line_two = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    identity_score = table.Column<int>(type: "integer", nullable: false),
                    total_request_execution_time = table.Column<int>(type: "integer", nullable: true),
                    endato_ce_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    request_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ce_query", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ce_query_endato_ce_person_endato_ce_person_id",
                        column: x => x.endato_ce_person_id,
                        principalTable: "endato_ce_person",
                        principalSchema: "endato",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "endato_ps_person",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    query_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    first_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    middle_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    last_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    suffix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ps_person", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ps_person_endato_ps_query_query_id",
                        column: x => x.query_id,
                        principalTable: "endato_ps_query",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ps_address",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deliverable = table.Column<bool>(type: "boolean", nullable: false),
                    is_merged_address = table.Column<bool>(type: "boolean", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    address_hash = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    house_number = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_pre_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    street_post_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    unit_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    city = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    county = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    zipcode4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    full_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    coordinates = table.Column<NpgsqlPoint>(type: "point", nullable: true),
                    phone_numbers = table.Column<string[]>(type: "text[]", nullable: true),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    first_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    public_first_seen_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_first_seen_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ps_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ps_address_endato_ps_person_endato_person_id",
                        column: x => x.endato_person_id,
                        principalTable: "endato_ps_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ps_email_address",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    is_premium = table.Column<bool>(type: "boolean", nullable: false),
                    non_business = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ps_email_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ps_email_address_endato_ps_person_endato_person_id",
                        column: x => x.endato_person_id,
                        principalTable: "endato_ps_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endato_ps_phone_number",
                schema: "endato",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endato_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    coordinates = table.Column<NpgsqlPoint>(type: "point", nullable: true),
                    order_number = table.Column<int>(type: "integer", nullable: false),
                    first_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reported_date = table.Column<DateOnly>(type: "date", nullable: false),
                    public_first_seen_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_first_seen_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endato_ps_phone_number", x => x.id);
                    table.ForeignKey(
                        name: "FK_endato_ps_phone_number_endato_ps_person_endato_person_id",
                        column: x => x.endato_person_id,
                        principalTable: "endato_ps_person",
                        principalSchema: "endato",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_endato_ce_address_endato_ce_person_id",
                table: "endato_ce_address",
                schema: "endato",
                column: "endato_ce_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_endato_ce_email_address_endato_ce_person_id",
                table: "endato_ce_email_address",
                schema: "endato",
                column: "endato_ce_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_endato_ce_phone_number_endato_ce_person_id",
                table: "endato_ce_phone_number",
                schema: "endato",
                column: "endato_ce_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_endato_ce_query_endato_ce_person_id",
                table: "endato_ce_query",
                schema: "endato",
                column: "endato_ce_person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_endato_ce_query_first_name_last_name_date_of_birth",
                table: "endato_ce_query",
                schema: "endato",
                columns: new[] { "first_name", "last_name", "date_of_birth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_endato_ps_address_endato_person_id",
                table: "endato_ps_address",
                schema: "endato",
                column: "endato_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_endato_ps_email_address_endato_person_id",
                table: "endato_ps_email_address",
                schema: "endato",
                column: "endato_person_id");

            migrationBuilder.CreateIndex(
                name: "IX_endato_ps_person_query_id",
                table: "endato_ps_person",
                schema: "endato",
                column: "query_id");

            migrationBuilder.CreateIndex(
                name: "IX_endato_ps_phone_number_endato_person_id",
                table: "endato_ps_phone_number",
                schema: "endato",
                column: "endato_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_endato_ps_query_first_name_last_name_date_of_birth",
                table: "endato_ps_query",
                schema: "endato",
                columns: new[] { "first_name", "last_name", "date_of_birth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "endato_ce_address",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ce_email_address",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ce_phone_number",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ce_query",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ps_address",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ps_email_address",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ps_phone_number",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ce_person",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ps_person",
                schema: "endato");

            migrationBuilder.DropTable(
                name: "endato_ps_query",
                schema: "endato");
        }
    }
}
