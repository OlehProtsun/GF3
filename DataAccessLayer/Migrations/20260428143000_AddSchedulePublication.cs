using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428143000_AddSchedulePublication")]
    public partial class AddSchedulePublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "publication_status",
                table: "schedule",
                type: "TEXT",
                nullable: false,
                defaultValue: "Private");

            migrationBuilder.CreateIndex(
                name: "ix_sched_publication_status",
                table: "schedule",
                column: "publication_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sched_publication_status",
                table: "schedule");

            migrationBuilder.DropColumn(
                name: "publication_status",
                table: "schedule");
        }
    }
}
