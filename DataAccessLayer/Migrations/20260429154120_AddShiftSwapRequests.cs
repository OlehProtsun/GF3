using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSwapRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_swap_request",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    from_employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    accepted_by_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    visibility = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Public"),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Open"),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_swap_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_shift_swap_request_employee_accepted_by_employee_id",
                        column: x => x.accepted_by_employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_swap_request_employee_from_employee_id",
                        column: x => x.from_employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_swap_request_employee_target_employee_id",
                        column: x => x.target_employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_swap_request_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shift_swap_request_schedule_slot_schedule_slot_id",
                        column: x => x.schedule_slot_id,
                        principalTable: "schedule_slot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_from_status",
                table: "shift_swap_request",
                columns: new[] { "from_employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_shift_swap_request_accepted_by_employee_id",
                table: "shift_swap_request",
                column: "accepted_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_schedule_status",
                table: "shift_swap_request",
                columns: new[] { "schedule_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_target_status",
                table: "shift_swap_request",
                columns: new[] { "target_employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shift_swap_open_slot",
                table: "shift_swap_request",
                columns: new[] { "schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_swap_request");
        }
    }
}
