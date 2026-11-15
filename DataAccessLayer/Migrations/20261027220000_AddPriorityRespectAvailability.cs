using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityRespectAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "respect_availability",
                table: "priority_generation_settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "priority_generation_settings",
                keyColumn: "id",
                keyValue: 1,
                column: "respect_availability",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "respect_availability",
                table: "priority_generation_settings");
        }
    }
}
