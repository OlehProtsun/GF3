using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddContractShiftApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "approved_employment_exception_approval_id",
                table: "shift_swap_request",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract_shift_approval_request",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    kind = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    original_day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    original_from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    original_to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    return_original_day_of_month = table.Column<int>(type: "INTEGER", nullable: true),
                    return_original_from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    return_original_to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    return_year = table.Column<int>(type: "INTEGER", nullable: true),
                    return_month = table.Column<int>(type: "INTEGER", nullable: true),
                    return_shop_id = table.Column<int>(type: "INTEGER", nullable: true),
                    shift_swap_request_id = table.Column<int>(type: "INTEGER", nullable: true),
                    resulting_shift_swap_request_id = table.Column<int>(type: "INTEGER", nullable: true),
                    target_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    request_kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    allow_partial_acceptance = table.Column<bool>(type: "INTEGER", nullable: false),
                    from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    return_schedule_id = table.Column<int>(type: "INTEGER", nullable: true),
                    return_schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: true),
                    return_from_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    return_to_time = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    reviewed_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    shop_id = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    failure_reason = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_shift_approval_request", x => x.id);
                    table.CheckConstraint("ck_contract_approval_kind", "kind IN ('claim','below_minimum_give')");
                    table.CheckConstraint("ck_contract_approval_period", "from_time LIKE '__:__' AND to_time LIKE '__:__' AND from_time < to_time");
                    table.CheckConstraint("ck_contract_approval_review", "status NOT IN ('approved','rejected') OR (reviewed_by_manager_id IS NOT NULL AND reviewed_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_contract_approval_status", "status IN ('pending','approved','rejected','cancelled','expired')");
                    table.ForeignKey(
                        name: "FK_contract_shift_approval_request_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_shift_approval_request_manager_account_reviewed_by_manager_id",
                        column: x => x.reviewed_by_manager_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_shift_approval_request_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_shift_approval_request_shift_swap_request_resulting_shift_swap_request_id",
                        column: x => x.resulting_shift_swap_request_id,
                        principalTable: "shift_swap_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contract_shift_approval_request_shift_swap_request_shift_swap_request_id",
                        column: x => x.shift_swap_request_id,
                        principalTable: "shift_swap_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shift_swap_request_approved_employment_exception_approval_id",
                table: "shift_swap_request",
                column: "approved_employment_exception_approval_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_employee_id_schedule_slot_id",
                table: "contract_shift_approval_request",
                columns: new[] { "employee_id", "schedule_slot_id" },
                unique: true,
                filter: "kind='below_minimum_give' AND status='pending'");

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_employee_id_shift_swap_request_id_from_time_to_time",
                table: "contract_shift_approval_request",
                columns: new[] { "employee_id", "shift_swap_request_id", "from_time", "to_time" },
                unique: true,
                filter: "kind='claim' AND status='pending'");

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_employee_id_status_created_at_utc",
                table: "contract_shift_approval_request",
                columns: new[] { "employee_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_resulting_shift_swap_request_id",
                table: "contract_shift_approval_request",
                column: "resulting_shift_swap_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_reviewed_by_manager_id",
                table: "contract_shift_approval_request",
                column: "reviewed_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_schedule_id_status_created_at_utc",
                table: "contract_shift_approval_request",
                columns: new[] { "schedule_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_shift_approval_request_shift_swap_request_id_status",
                table: "contract_shift_approval_request",
                columns: new[] { "shift_swap_request_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "FK_shift_swap_request_contract_shift_approval_request_approved_employment_exception_approval_id",
                table: "shift_swap_request",
                column: "approved_employment_exception_approval_id",
                principalTable: "contract_shift_approval_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shift_swap_request_contract_shift_approval_request_approved_employment_exception_approval_id",
                table: "shift_swap_request");

            migrationBuilder.DropTable(
                name: "contract_shift_approval_request");

            migrationBuilder.DropIndex(
                name: "IX_shift_swap_request_approved_employment_exception_approval_id",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "approved_employment_exception_approval_id",
                table: "shift_swap_request");
        }
    }
}
