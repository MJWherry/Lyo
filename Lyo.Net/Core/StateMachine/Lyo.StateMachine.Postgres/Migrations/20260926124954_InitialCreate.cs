using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.StateMachine.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "state_machine");

            migrationBuilder.CreateTable(
                name: "instance",
                schema: "state_machine",
                columns: table => new
                {
                    machine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instance_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    entered_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state_machine_instance", x => new { x.machine_name, x.instance_key });
                });

            migrationBuilder.CreateTable(
                name: "transition",
                schema: "state_machine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instance_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_state = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    occurred_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state_machine_transition", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_state_machine_instance_machine_state",
                schema: "state_machine",
                table: "instance",
                columns: new[] { "machine_name", "state" });

            migrationBuilder.CreateIndex(
                name: "ux_state_machine_transition_instance_version",
                schema: "state_machine",
                table: "transition",
                columns: new[] { "machine_name", "instance_key", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instance",
                schema: "state_machine");

            migrationBuilder.DropTable(
                name: "transition",
                schema: "state_machine");
        }
    }
}
