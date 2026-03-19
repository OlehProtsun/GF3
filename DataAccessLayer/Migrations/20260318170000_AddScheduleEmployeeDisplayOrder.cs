using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260318170000_AddScheduleEmployeeDisplayOrder")]
    public partial class AddScheduleEmployeeDisplayOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "schedule_employee",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH ordered_employees AS (
                    SELECT
                        se.id,
                        ROW_NUMBER() OVER (
                            PARTITION BY se.schedule_id
                            ORDER BY
                                COALESCE(e.first_name, ''),
                                COALESCE(e.last_name, ''),
                                se.employee_id,
                                se.id
                        ) - 1 AS display_order
                    FROM schedule_employee AS se
                    LEFT JOIN employee AS e ON e.id = se.employee_id
                )
                UPDATE schedule_employee
                SET display_order = (
                    SELECT ordered_employees.display_order
                    FROM ordered_employees
                    WHERE ordered_employees.id = schedule_employee.id
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_order",
                table: "schedule_employee");
        }
    }
}
