using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class DropScheduleStatusAndComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment",
                table: "schedule");

            migrationBuilder.DropColumn(
                name: "status",
                table: "schedule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "schedule",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "schedule",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
