using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeUiState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_notification_read",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    notification_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    read_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_notification_read", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_notification_read_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_schedule_column_preference",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    column_order_json = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_schedule_column_preference", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_schedule_column_preference_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_schedule_column_preference_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_emp_notification_read_emp_time",
                table: "employee_notification_read",
                columns: new[] { "employee_id", "read_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_emp_notification_read_emp_notification",
                table: "employee_notification_read",
                columns: new[] { "employee_id", "notification_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_schedule_column_preference_schedule_id",
                table: "employee_schedule_column_preference",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "ux_emp_schedule_column_pref_emp_schedule",
                table: "employee_schedule_column_preference",
                columns: new[] { "employee_id", "schedule_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_notification_read");

            migrationBuilder.DropTable(
                name: "employee_schedule_column_preference");
        }
    }
}
