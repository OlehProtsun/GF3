using System;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423120000_AddEmployeePasswordResetCodes")]
    public partial class AddEmployeePasswordResetCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_reset_code_hash",
                table: "employee_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_expires_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_requested_at_utc",
                table: "employee_account",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_reset_code_hash",
                table: "employee_account");

            migrationBuilder.DropColumn(
                name: "password_reset_expires_at_utc",
                table: "employee_account");

            migrationBuilder.DropColumn(
                name: "password_reset_requested_at_utc",
                table: "employee_account");
        }
    }
}
