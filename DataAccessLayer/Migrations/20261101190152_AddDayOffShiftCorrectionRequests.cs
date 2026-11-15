using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddDayOffShiftCorrectionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shift_correction_pending_slot",
                table: "shift_correction_request");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shift_correction_time_format",
                table: "shift_correction_request");

            migrationBuilder.AlterColumn<int>(
                name: "schedule_slot_id",
                table: "shift_correction_request",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "original_to_time",
                table: "shift_correction_request",
                type: "TEXT",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "original_from_time",
                table: "shift_correction_request",
                type: "TEXT",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5);

            migrationBuilder.CreateIndex(
                name: "ux_shift_correction_pending_day_off",
                table: "shift_correction_request",
                columns: new[] { "schedule_id", "employee_id", "day_of_month" },
                unique: true,
                filter: "status = 'Pending' AND original_from_time IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_shift_correction_pending_slot",
                table: "shift_correction_request",
                columns: new[] { "schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Pending' AND schedule_slot_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shift_correction_time_format",
                table: "shift_correction_request",
                sql: "((original_from_time IS NULL AND original_to_time IS NULL) OR (original_from_time IS NOT NULL AND original_to_time IS NOT NULL AND schedule_slot_id > 0 AND schedule_slot_id IS NOT NULL AND original_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_from_time < '24:00' AND original_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_to_time < '24:00')) AND requested_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_from_time < '24:00' AND requested_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_to_time < '24:00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Every day-off snapshot, including approved history, blocks a lossless rollback.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE gf3_day_off_rollback_guard (
                    blocked INTEGER CONSTRAINT "Rollback blocked: day-off correction history exists" CHECK (blocked = 0)
                );
                INSERT INTO gf3_day_off_rollback_guard
                    SELECT EXISTS(SELECT 1 FROM shift_correction_request
                        WHERE original_from_time IS NULL OR original_to_time IS NULL OR schedule_slot_id IS NULL);
                DROP TABLE gf3_day_off_rollback_guard;
                """);

            migrationBuilder.DropIndex(
                name: "ux_shift_correction_pending_day_off",
                table: "shift_correction_request");

            migrationBuilder.DropIndex(
                name: "ux_shift_correction_pending_slot",
                table: "shift_correction_request");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shift_correction_time_format",
                table: "shift_correction_request");

            migrationBuilder.AlterColumn<int>(
                name: "schedule_slot_id",
                table: "shift_correction_request",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "original_to_time",
                table: "shift_correction_request",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "original_from_time",
                table: "shift_correction_request",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_shift_correction_pending_slot",
                table: "shift_correction_request",
                columns: new[] { "schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shift_correction_time_format",
                table: "shift_correction_request",
                sql: "original_from_time LIKE '__:__' AND original_to_time LIKE '__:__' AND requested_from_time LIKE '__:__' AND requested_to_time LIKE '__:__'");
        }
    }
}
