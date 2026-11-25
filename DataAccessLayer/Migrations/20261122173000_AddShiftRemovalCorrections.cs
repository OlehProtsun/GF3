using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

public partial class AddShiftRemovalCorrections : Migration
{
    private const string TableName = "shift_correction_request";
    private const string TimeConstraintName = "ck_shift_correction_time_format";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(TimeConstraintName, TableName);
        migrationBuilder.AddColumn<string>("operation", TableName, type: "TEXT", nullable: false, defaultValue: "ChangeTimes");
        migrationBuilder.AddColumn<string>("removal_snapshot_json", TableName, type: "TEXT", nullable: true);
        migrationBuilder.AlterColumn<string>("requested_from_time", TableName, type: "TEXT", maxLength: 5, nullable: true,
            oldClrType: typeof(string), oldType: "TEXT", oldMaxLength: 5);
        migrationBuilder.AlterColumn<string>("requested_to_time", TableName, type: "TEXT", maxLength: 5, nullable: true,
            oldClrType: typeof(string), oldType: "TEXT", oldMaxLength: 5);
        migrationBuilder.AddCheckConstraint(TimeConstraintName, TableName, "(operation = 'RemoveShift' AND removal_snapshot_json IS NOT NULL AND schedule_slot_id IS NULL AND original_from_time IS NULL AND original_to_time IS NULL AND requested_from_time IS NULL AND requested_to_time IS NULL) OR (operation = 'ChangeTimes' AND removal_snapshot_json IS NULL AND ((original_from_time IS NULL AND original_to_time IS NULL) OR (original_from_time IS NOT NULL AND original_to_time IS NOT NULL AND schedule_slot_id > 0 AND schedule_slot_id IS NOT NULL AND original_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_from_time < '24:00' AND original_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_to_time < '24:00')) AND requested_from_time IS NOT NULL AND requested_to_time IS NOT NULL AND requested_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_from_time < '24:00' AND requested_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_to_time < '24:00')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(TimeConstraintName, TableName);
        migrationBuilder.DropColumn("removal_snapshot_json", TableName);
        migrationBuilder.DropColumn("operation", TableName);
        migrationBuilder.AlterColumn<string>("requested_from_time", TableName, type: "TEXT", maxLength: 5, nullable: false,
            oldClrType: typeof(string), oldType: "TEXT", oldMaxLength: 5, oldNullable: true);
        migrationBuilder.AlterColumn<string>("requested_to_time", TableName, type: "TEXT", maxLength: 5, nullable: false,
            oldClrType: typeof(string), oldType: "TEXT", oldMaxLength: 5, oldNullable: true);
        migrationBuilder.AddCheckConstraint(TimeConstraintName, TableName, "((original_from_time IS NULL AND original_to_time IS NULL) OR (original_from_time IS NOT NULL AND original_to_time IS NOT NULL AND schedule_slot_id > 0 AND schedule_slot_id IS NOT NULL AND original_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_from_time < '24:00' AND original_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND original_to_time < '24:00')) AND requested_from_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_from_time < '24:00' AND requested_to_time GLOB '[0-2][0-9]:[0-5][0-9]' AND requested_to_time < '24:00'");
    }
}
