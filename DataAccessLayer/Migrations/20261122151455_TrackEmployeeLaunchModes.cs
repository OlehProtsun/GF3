using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class TrackEmployeeLaunchModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_browser_launch_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_pwa_launch_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_browser_launch_at_utc",
                table: "employee_account");

            migrationBuilder.DropColumn(
                name: "last_pwa_launch_at_utc",
                table: "employee_account");
        }
    }
}
