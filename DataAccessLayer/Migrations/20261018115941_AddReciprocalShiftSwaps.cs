using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddReciprocalShiftSwaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "request_kind",
                table: "shift_swap_request",
                type: "TEXT",
                nullable: false,
                defaultValue: "Give");

            migrationBuilder.AddColumn<string>(
                name: "return_from_time",
                table: "shift_swap_request",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_schedule_id",
                table: "shift_swap_request",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_schedule_slot_id",
                table: "shift_swap_request",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_to_time",
                table: "shift_swap_request",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_kind",
                table: "shift_swap_history",
                type: "TEXT",
                nullable: false,
                defaultValue: "Give");

            migrationBuilder.AddColumn<string>(
                name: "return_after_snapshot_json",
                table: "shift_swap_history",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_before_snapshot_json",
                table: "shift_swap_history",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_day_of_month",
                table: "shift_swap_history",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_from_time",
                table: "shift_swap_history",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_month",
                table: "shift_swap_history",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_schedule_id",
                table: "shift_swap_history",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_schedule_name",
                table: "shift_swap_history",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_shop_name",
                table: "shift_swap_history",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_to_time",
                table: "shift_swap_history",
                type: "TEXT",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "return_year",
                table: "shift_swap_history",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_return_schedule_status",
                table: "shift_swap_request",
                columns: new[] { "return_schedule_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_swap_open_return_slot",
                table: "shift_swap_request",
                columns: new[] { "return_schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Open' AND request_kind = 'Exchange' AND return_schedule_slot_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shift_swap_return_schedule_status",
                table: "shift_swap_request");

            migrationBuilder.DropIndex(
                name: "ux_shift_swap_open_return_slot",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "request_kind",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "return_from_time",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "return_schedule_id",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "return_schedule_slot_id",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "return_to_time",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "request_kind",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_after_snapshot_json",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_before_snapshot_json",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_day_of_month",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_from_time",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_month",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_schedule_id",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_schedule_name",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_shop_name",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_to_time",
                table: "shift_swap_history");

            migrationBuilder.DropColumn(
                name: "return_year",
                table: "shift_swap_history");
        }
    }
}
