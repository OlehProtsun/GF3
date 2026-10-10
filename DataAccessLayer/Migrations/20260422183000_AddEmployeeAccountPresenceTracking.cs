using System;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422183000_AddEmployeeAccountPresenceTracking")]
    public partial class AddEmployeeAccountPresenceTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_login_at_utc",
                table: "employee_account");

            migrationBuilder.DropColumn(
                name: "last_seen_at_utc",
                table: "employee_account");
        }
    }
}
