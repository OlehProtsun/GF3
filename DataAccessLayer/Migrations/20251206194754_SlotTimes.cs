using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class SlotTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_schedule_slot_schedule_id_day_of_month_shift_no_slot_no",
                table: "schedule_slot");

            migrationBuilder.DropIndex(
                name: "ix_slot_sched_day_shift",
                table: "schedule_slot");

            migrationBuilder.DropIndex(
                name: "ux_slot_unique_emp_per_shift",
                table: "schedule_slot");

            migrationBuilder.DropIndex(
                name: "ux_slot_unique_emp_per_shift1",
                table: "schedule_slot");

            migrationBuilder.DropCheckConstraint(
                name: "ck_schedule_slot_shift_no",
                table: "schedule_slot");

            migrationBuilder.AlterColumn<int>(
                name: "shift_no",
                table: "schedule_slot",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "from_time",
                table: "schedule_slot",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "to_time",
                table: "schedule_slot",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slot_schedule_id_day_of_month_from_time_to_time_slot_no",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "from_time", "to_time", "slot_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_time",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "from_time", "to_time", "employee_id" },
                unique: true,
                filter: "employee_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_time1",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "from_time", "to_time", "employee_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_schedule_slot_time_format",
                table: "schedule_slot",
                sql: "from_time LIKE '__:__' AND to_time LIKE '__:__'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_schedule_slot_time_order",
                table: "schedule_slot",
                sql: "from_time < to_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_schedule_slot_schedule_id_day_of_month_from_time_to_time_slot_no",
                table: "schedule_slot");

            migrationBuilder.DropIndex(
                name: "ux_slot_unique_emp_per_time",
                table: "schedule_slot");

            migrationBuilder.DropIndex(
                name: "ux_slot_unique_emp_per_time1",
                table: "schedule_slot");

            migrationBuilder.DropCheckConstraint(
                name: "ck_schedule_slot_time_format",
                table: "schedule_slot");

            migrationBuilder.DropCheckConstraint(
                name: "ck_schedule_slot_time_order",
                table: "schedule_slot");

            migrationBuilder.DropColumn(
                name: "from_time",
                table: "schedule_slot");

            migrationBuilder.DropColumn(
                name: "to_time",
                table: "schedule_slot");

            migrationBuilder.AlterColumn<int>(
                name: "shift_no",
                table: "schedule_slot",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slot_schedule_id_day_of_month_shift_no_slot_no",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "slot_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_slot_sched_day_shift",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no" });

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_shift",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "employee_id" },
                unique: true,
                filter: "employee_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_shift1",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "employee_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_schedule_slot_shift_no",
                table: "schedule_slot",
                sql: "shift_no IN (1,2)");
        }
    }
}
