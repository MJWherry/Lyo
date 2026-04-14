using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Lyo.People.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "people");

            migrationBuilder.CreateTable(
                name: "address",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    house_number = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_pre_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    street_post_direction = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    unit = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    unit_type = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    street_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    street_address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    county = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    zipcode4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    full_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    coordinates = table.Column<NpgsqlPoint>(type: "point", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_address", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_address",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_address", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name_prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    first_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    last_name = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    name_suffix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Manual"),
                    preferred_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    maiden_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    sex = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    nationality = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    preferred_language_bcp47 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    race = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    marital_status = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    disability_status = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    veteran_status = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    place_of_birth_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    emergency_contact_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    current_company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    citizenship_json = table.Column<string>(type: "jsonb", nullable: true),
                    preferences_json = table.Column<string>(type: "jsonb", nullable: true),
                    custom_fields_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phone_number",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    country_code_string = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    technology_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_number", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contact_email_address",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    opted_out_of_marketing = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_email_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_contact_email_address_email_address_email_address_id",
                        column: x => x.email_address_id,
                        principalSchema: "people",
                        principalTable: "email_address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contact_email_address_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employment",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supervisor_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    salary = table.Column<decimal>(type: "numeric", nullable: true),
                    salary_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employment", x => x.id);
                    table.ForeignKey(
                        name: "FK_employment_address_company_address_id",
                        column: x => x.company_address_id,
                        principalSchema: "people",
                        principalTable: "address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employment_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identification",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    issuing_country = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    issuing_authority = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identification", x => x.id);
                    table.ForeignKey(
                        name: "FK_identification_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contact_address",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_address", x => x.id);
                    table.ForeignKey(
                        name: "FK_contact_address_address_address_id",
                        column: x => x.address_id,
                        principalSchema: "people",
                        principalTable: "address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contact_address_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "person_relationship",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_relationship", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_relationship_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_media_profile",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    username = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_media_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_social_media_profile_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contact_phone_number",
                schema: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_phone_number", x => x.id);
                    table.ForeignKey(
                        name: "FK_contact_phone_number_person_person_id",
                        column: x => x.person_id,
                        principalSchema: "people",
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contact_phone_number_phone_number_phone_number_id",
                        column: x => x.phone_number_id,
                        principalSchema: "people",
                        principalTable: "phone_number",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_country_code",
                schema: "people",
                table: "address",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_contact_email_address_email_address_id",
                schema: "people",
                table: "contact_email_address",
                column: "email_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_email_address_person_id",
                schema: "people",
                table: "contact_email_address",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_phone_number_person_id",
                schema: "people",
                table: "contact_phone_number",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_phone_number_phone_number_id",
                schema: "people",
                table: "contact_phone_number",
                column: "phone_number_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_address_email",
                schema: "people",
                table: "email_address",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_employment_company_address_id",
                schema: "people",
                table: "employment",
                column: "company_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_employment_company_name",
                schema: "people",
                table: "employment",
                column: "company_name");

            migrationBuilder.CreateIndex(
                name: "ix_employment_person_id",
                schema: "people",
                table: "employment",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_identification_person_id",
                schema: "people",
                table: "identification",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_identification_type_number",
                schema: "people",
                table: "identification",
                columns: new[] { "type", "number" });

            migrationBuilder.CreateIndex(
                name: "ix_person_created_timestamp",
                schema: "people",
                table: "person",
                column: "created_timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_person_first_name",
                schema: "people",
                table: "person",
                column: "first_name");

            migrationBuilder.CreateIndex(
                name: "ix_person_is_active",
                schema: "people",
                table: "person",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_person_last_name",
                schema: "people",
                table: "person",
                column: "last_name");

            migrationBuilder.CreateIndex(
                name: "ix_person_last_name_first_name",
                schema: "people",
                table: "person",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_address_address_id",
                schema: "people",
                table: "contact_address",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_address_person_id",
                schema: "people",
                table: "contact_address",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_relationship_person_id",
                schema: "people",
                table: "person_relationship",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_relationship_related_person_id",
                schema: "people",
                table: "person_relationship",
                column: "related_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_phone_number_number",
                schema: "people",
                table: "phone_number",
                column: "number");

            migrationBuilder.CreateIndex(
                name: "ix_social_media_profile_person_id",
                schema: "people",
                table: "social_media_profile",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_social_media_profile_platform_username",
                schema: "people",
                table: "social_media_profile",
                columns: new[] { "platform", "username" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_email_address",
                schema: "people");

            migrationBuilder.DropTable(
                name: "contact_phone_number",
                schema: "people");

            migrationBuilder.DropTable(
                name: "employment",
                schema: "people");

            migrationBuilder.DropTable(
                name: "identification",
                schema: "people");

            migrationBuilder.DropTable(
                name: "contact_address",
                schema: "people");

            migrationBuilder.DropTable(
                name: "person_relationship",
                schema: "people");

            migrationBuilder.DropTable(
                name: "social_media_profile",
                schema: "people");

            migrationBuilder.DropTable(
                name: "email_address",
                schema: "people");

            migrationBuilder.DropTable(
                name: "phone_number",
                schema: "people");

            migrationBuilder.DropTable(
                name: "address",
                schema: "people");

            migrationBuilder.DropTable(
                name: "person",
                schema: "people");
        }
    }
}
