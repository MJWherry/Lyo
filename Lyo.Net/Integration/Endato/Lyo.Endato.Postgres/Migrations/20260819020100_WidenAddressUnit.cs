using Lyo.Endato.Postgres.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Endato.Postgres.Migrations;

[DbContext(typeof(EndatoDbContext))]
[Migration("20260819020100_WidenAddressUnit")]
public class WidenAddressUnit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "unit",
            schema: "endato",
            table: "endato_ps_address",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "unit",
            schema: "endato",
            table: "endato_ce_address",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "unit",
            schema: "endato",
            table: "endato_ps_address",
            type: "character varying(8)",
            maxLength: 8,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "unit",
            schema: "endato",
            table: "endato_ce_address",
            type: "character varying(8)",
            maxLength: 8,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32,
            oldNullable: true);
    }
}
