using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class DropDuplicateScheduleSlotEmployeeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_slot_unique_emp_per_time1",
                table: "schedule_slot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_time1",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "from_time", "to_time", "employee_id" },
                unique: true);
        }
    }
}
