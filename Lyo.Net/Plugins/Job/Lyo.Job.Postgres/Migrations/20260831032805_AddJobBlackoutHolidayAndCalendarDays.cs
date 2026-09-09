using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lyo.Job.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddJobBlackoutHolidayAndCalendarDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "days_of_month",
                schema: "job",
                table: "job_blackout_window",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "holiday_slug",
                schema: "job",
                table: "job_blackout_window",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "include_observed_date",
                schema: "job",
                table: "job_blackout_window",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "month_flags",
                schema: "job",
                table: "job_blackout_window",
                type: "character varying(108)",
                maxLength: 108,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "days_of_month",
                schema: "job",
                table: "job_blackout_window");

            migrationBuilder.DropColumn(
                name: "holiday_slug",
                schema: "job",
                table: "job_blackout_window");

            migrationBuilder.DropColumn(
                name: "include_observed_date",
                schema: "job",
                table: "job_blackout_window");

            migrationBuilder.DropColumn(
                name: "month_flags",
                schema: "job",
                table: "job_blackout_window");
        }
    }
}
