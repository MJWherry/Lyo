using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.StateMachine.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDataTimersDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_json",
                schema: "state_machine",
                table: "instance",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "due_at",
                schema: "state_machine",
                table: "instance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "definition",
                schema: "state_machine",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    created_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state_machine_definition", x => new { x.name, x.revision });
                });

            migrationBuilder.CreateIndex(
                name: "ix_state_machine_instance_machine_due",
                schema: "state_machine",
                table: "instance",
                columns: new[] { "machine_name", "due_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "definition",
                schema: "state_machine");

            migrationBuilder.DropIndex(
                name: "ix_state_machine_instance_machine_due",
                schema: "state_machine",
                table: "instance");

            migrationBuilder.DropColumn(
                name: "data_json",
                schema: "state_machine",
                table: "instance");

            migrationBuilder.DropColumn(
                name: "due_at",
                schema: "state_machine",
                table: "instance");
        }
    }
}
