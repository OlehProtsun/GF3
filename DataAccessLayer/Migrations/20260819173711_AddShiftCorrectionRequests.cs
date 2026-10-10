using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftCorrectionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manager_shift_correction_setting",
                columns: table => new
                {
                    manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    highlight_color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manager_shift_correction_setting", x => x.manager_account_id);
                    table.ForeignKey(
                        name: "FK_manager_shift_correction_setting_manager_account_manager_account_id",
                        column: x => x.manager_account_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_correction_request",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    original_from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    original_to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    requested_from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    requested_to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Pending"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    reviewed_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_correction_request", x => x.id);
                    table.CheckConstraint("ck_shift_correction_dom", "day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_shift_correction_requested_order", "requested_from_time < requested_to_time");
                    table.CheckConstraint("ck_shift_correction_time_format", "original_from_time LIKE '__:__' AND original_to_time LIKE '__:__' AND requested_from_time LIKE '__:__' AND requested_to_time LIKE '__:__'");
                    table.ForeignKey(
                        name: "FK_shift_correction_request_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shift_correction_request_manager_account_reviewed_by_manager_id",
                        column: x => x.reviewed_by_manager_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_shift_correction_request_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_correction_employee_status",
                table: "shift_correction_request",
                columns: new[] { "employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_shift_correction_request_reviewed_by_manager_id",
                table: "shift_correction_request",
                column: "reviewed_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_correction_schedule_status",
                table: "shift_correction_request",
                columns: new[] { "schedule_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_correction_pending_slot",
                table: "shift_correction_request",
                columns: new[] { "schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manager_shift_correction_setting");

            migrationBuilder.DropTable(
                name: "shift_correction_request");
        }
    }
}
