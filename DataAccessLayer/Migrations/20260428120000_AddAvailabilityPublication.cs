using System;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428120000_AddAvailabilityPublication")]
    public partial class AddAvailabilityPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "publication_status",
                table: "availability_group",
                type: "TEXT",
                nullable: false,
                defaultValue: "Private");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "visible_from_utc",
                table: "availability_group",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "visible_to_utc",
                table: "availability_group",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "employee_last_modified_at_utc",
                table: "availability_group_member",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_avail_group_publication_visibility",
                table: "availability_group",
                columns: new[] { "publication_status", "visible_from_utc", "visible_to_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_avail_group_publication_visibility",
                table: "availability_group");

            migrationBuilder.DropColumn(
                name: "employee_last_modified_at_utc",
                table: "availability_group_member");

            migrationBuilder.DropColumn(
                name: "visible_to_utc",
                table: "availability_group");

            migrationBuilder.DropColumn(
                name: "visible_from_utc",
                table: "availability_group");

            migrationBuilder.DropColumn(
                name: "publication_status",
                table: "availability_group");
        }
    }
}
