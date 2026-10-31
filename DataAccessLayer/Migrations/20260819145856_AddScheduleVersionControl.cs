using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleVersionControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_version",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    parent_version_id = table.Column<int>(type: "INTEGER", nullable: true),
                    version_number = table.Column<int>(type: "INTEGER", nullable: false),
                    branch_name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_by_manager_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    employee_count = table.Column<int>(type: "INTEGER", nullable: false),
                    slot_count = table.Column<int>(type: "INTEGER", nullable: false),
                    cell_style_count = table.Column<int>(type: "INTEGER", nullable: false),
                    snapshot_json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_version", x => x.id);
                    table.CheckConstraint("ck_schedule_version_counts", "employee_count >= 0 AND slot_count >= 0 AND cell_style_count >= 0");
                    table.CheckConstraint("ck_schedule_version_number", "version_number >= 1");
                    table.ForeignKey(
                        name: "FK_schedule_version_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_version_schedule_version_parent_version_id",
                        column: x => x.parent_version_id,
                        principalTable: "schedule_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "schedule_version_state",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    current_version_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_version_state", x => x.schedule_id);
                    table.ForeignKey(
                        name: "FK_schedule_version_state_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_version_state_schedule_version_current_version_id",
                        column: x => x.current_version_id,
                        principalTable: "schedule_version",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_version_branch",
                table: "schedule_version",
                columns: new[] { "schedule_id", "branch_name", "version_number" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_version_parent_version_id",
                table: "schedule_version",
                column: "parent_version_id");

            migrationBuilder.CreateIndex(
                name: "ux_schedule_version_number",
                table: "schedule_version",
                columns: new[] { "schedule_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_version_state_current_version_id",
                table: "schedule_version_state",
                column: "current_version_id");

            migrationBuilder.Sql("""
                INSERT INTO schedule_version
                    (schedule_id, parent_version_id, version_number, branch_name, created_at_utc,
                     created_by_manager_id, created_by_manager_name, employee_count, slot_count,
                     cell_style_count, snapshot_json)
                SELECT
                    s.id,
                    NULL,
                    1,
                    'main',
                    strftime('%Y-%m-%d %H:%M:%f+00:00', 'now'),
                    NULL,
                    'Production import',
                    (SELECT COUNT(*) FROM schedule_employee se WHERE se.schedule_id = s.id),
                    (SELECT COUNT(*) FROM schedule_slot ss WHERE ss.schedule_id = s.id),
                    (SELECT COUNT(*) FROM schedule_cell_style scs WHERE scs.schedule_id = s.id),
                    json_object(
                        'shopId', s.shop_id,
                        'name', s.name,
                        'year', s.year,
                        'month', s.month,
                        'publicationStatus', s.publication_status,
                        'allowSwap', CASE WHEN s.allow_swap = 1 THEN json('true') ELSE json('false') END,
                        'peoplePerShift', s.people_per_shift,
                        'shift1Time', s.shift1_time,
                        'shift2Time', s.shift2_time,
                        'maxHoursPerEmpMonth', s.max_hours_per_emp_month,
                        'maxConsecutiveDays', s.max_consecutive_days,
                        'maxConsecutiveFull', s.max_consecutive_full,
                        'maxFullPerMonth', s.max_full_per_month,
                        'note', s.note,
                        'availabilityGroupId', s.availability_group_id,
                        'employees', json(COALESCE((
                            SELECT json_group_array(json_object(
                                'employeeId', ordered_se.employee_id,
                                'minHoursMonth', ordered_se.min_hours_month,
                                'displayOrder', ordered_se.display_order))
                            FROM (
                                SELECT employee_id, min_hours_month, display_order
                                FROM schedule_employee
                                WHERE schedule_id = s.id
                                ORDER BY display_order, id
                            ) ordered_se
                        ), '[]')),
                        'slots', json(COALESCE((
                            SELECT json_group_array(json_object(
                                'dayOfMonth', ordered_ss.day_of_month,
                                'slotNo', ordered_ss.slot_no,
                                'fromTime', ordered_ss.from_time,
                                'toTime', ordered_ss.to_time,
                                'employeeId', ordered_ss.employee_id,
                                'status', ordered_ss.status))
                            FROM (
                                SELECT day_of_month, slot_no, from_time, to_time, employee_id, status
                                FROM schedule_slot
                                WHERE schedule_id = s.id
                                ORDER BY day_of_month, slot_no, id
                            ) ordered_ss
                        ), '[]')),
                        'cellStyles', json(COALESCE((
                            SELECT json_group_array(json_object(
                                'dayOfMonth', ordered_scs.day_of_month,
                                'employeeId', ordered_scs.employee_id,
                                'backgroundColorArgb', ordered_scs.background_color_argb,
                                'textColorArgb', ordered_scs.text_color_argb))
                            FROM (
                                SELECT day_of_month, employee_id, background_color_argb, text_color_argb
                                FROM schedule_cell_style
                                WHERE schedule_id = s.id
                                ORDER BY day_of_month, employee_id, id
                            ) ordered_scs
                        ), '[]'))
                    )
                FROM schedule s;

                INSERT INTO schedule_version_state (schedule_id, current_version_id)
                SELECT schedule_id, id
                FROM schedule_version
                WHERE version_number = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_version_state");

            migrationBuilder.DropTable(
                name: "schedule_version");
        }
    }
}
