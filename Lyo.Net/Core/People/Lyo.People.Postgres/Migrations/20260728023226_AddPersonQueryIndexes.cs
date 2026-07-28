using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.People.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_person_first_name_last_name_date_of_birth",
                schema: "people",
                table: "person",
                columns: new[] { "first_name", "last_name", "date_of_birth" });

            migrationBuilder.CreateIndex(
                name: "ix_person_last_name_first_name_id",
                schema: "people",
                table: "person",
                columns: new[] { "last_name", "first_name", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_person_source_entity_type",
                schema: "people",
                table: "person",
                column: "source_entity_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_person_first_name_last_name_date_of_birth",
                schema: "people",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_person_last_name_first_name_id",
                schema: "people",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_person_source_entity_type",
                schema: "people",
                table: "person");
        }
    }
}
