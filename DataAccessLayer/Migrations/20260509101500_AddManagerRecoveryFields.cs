using System;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260509101500_AddManagerRecoveryFields")]
    public partial class AddManagerRecoveryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "recovery_email",
                table: "manager_account",
                type: "TEXT",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_code_hash",
                table: "manager_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_requested_at_utc",
                table: "manager_account",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_expires_at_utc",
                table: "manager_account",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recovery_email",
                table: "manager_account");

            migrationBuilder.DropColumn(
                name: "password_reset_code_hash",
                table: "manager_account");

            migrationBuilder.DropColumn(
                name: "password_reset_requested_at_utc",
                table: "manager_account");

            migrationBuilder.DropColumn(
                name: "password_reset_expires_at_utc",
                table: "manager_account");
        }
    }
}
