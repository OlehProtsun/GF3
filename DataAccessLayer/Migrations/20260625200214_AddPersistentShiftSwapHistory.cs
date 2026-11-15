using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentShiftSwapHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_swap_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    source_shift_swap_request_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    container_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    shop_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    from_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    from_employee_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    target_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    target_employee_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    accepted_by_employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    accepted_by_employee_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_manager_created = table.Column<bool>(type: "INTEGER", nullable: false),
                    manual_column_id = table.Column<int>(type: "INTEGER", nullable: true),
                    manual_column_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    before_snapshot_json = table.Column<string>(type: "TEXT", nullable: false),
                    after_snapshot_json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_swap_history", x => x.id);
                    table.CheckConstraint("ck_shift_swap_history_dom", "day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_shift_swap_history_month", "month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_shift_swap_history_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_history_schedule_accepted",
                table: "shift_swap_history",
                columns: new[] { "schedule_id", "accepted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_swap_history_source_request",
                table: "shift_swap_history",
                column: "source_shift_swap_request_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO shift_swap_history (
                    source_shift_swap_request_id,
                    schedule_id,
                    schedule_slot_id,
                    schedule_name,
                    container_name,
                    shop_name,
                    year,
                    month,
                    day_of_month,
                    from_time,
                    to_time,
                    from_employee_id,
                    from_employee_name,
                    target_employee_id,
                    target_employee_name,
                    accepted_by_employee_id,
                    accepted_by_employee_name,
                    created_at_utc,
                    accepted_at_utc,
                    is_manager_created,
                    manual_column_id,
                    manual_column_name,
                    before_snapshot_json,
                    after_snapshot_json
                )
                SELECT
                    request.id,
                    request.schedule_id,
                    request.schedule_slot_id,
                    schedule.name,
                    container.name,
                    shop.name,
                    schedule.year,
                    schedule.month,
                    slot.day_of_month,
                    COALESCE(NULLIF(request.offered_from_time, ''), slot.from_time),
                    COALESCE(NULLIF(request.offered_to_time, ''), slot.to_time),
                    request.from_employee_id,
                    CASE
                        WHEN request.is_manager_created = 1 THEN 'Custom column'
                        ELSE COALESCE(NULLIF(TRIM(from_employee.first_name || ' ' || from_employee.last_name), ''), 'Employee #' || request.from_employee_id)
                    END,
                    request.target_employee_id,
                    CASE
                        WHEN request.target_employee_id IS NULL THEN NULL
                        ELSE COALESCE(NULLIF(TRIM(target_employee.first_name || ' ' || target_employee.last_name), ''), 'Employee #' || request.target_employee_id)
                    END,
                    request.accepted_by_employee_id,
                    COALESCE(NULLIF(TRIM(accepted_employee.first_name || ' ' || accepted_employee.last_name), ''), 'Employee #' || request.accepted_by_employee_id),
                    request.created_at_utc,
                    COALESCE(request.accepted_at_utc, request.created_at_utc),
                    request.is_manager_created,
                    request.manual_column_id,
                    CASE WHEN request.is_manager_created = 1 THEN 'Custom column' ELSE NULL END,
                    '{"rows":[]}',
                    '{"rows":[]}'
                FROM shift_swap_request AS request
                INNER JOIN schedule ON schedule.id = request.schedule_id
                INNER JOIN schedule_slot AS slot ON slot.id = request.schedule_slot_id
                INNER JOIN container ON container.id = schedule.container_id
                INNER JOIN shop ON shop.id = schedule.shop_id
                LEFT JOIN employee AS from_employee ON from_employee.id = request.from_employee_id
                LEFT JOIN employee AS target_employee ON target_employee.id = request.target_employee_id
                INNER JOIN employee AS accepted_employee ON accepted_employee.id = request.accepted_by_employee_id
                WHERE request.status = 'Accepted'
                  AND request.accepted_by_employee_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_swap_history");
        }
    }
}
