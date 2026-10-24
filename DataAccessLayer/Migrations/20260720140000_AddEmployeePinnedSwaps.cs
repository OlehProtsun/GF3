using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260720140000_AddEmployeePinnedSwaps")]
public partial class AddEmployeePinnedSwaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "employee_pinned_swap",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                shift_swap_id = table.Column<int>(type: "INTEGER", nullable: false),
                pinned_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_pinned_swap", item => item.id);
                table.ForeignKey(
                    name: "FK_employee_pinned_swap_employee_employee_id",
                    column: item => item.employee_id,
                    principalTable: "employee",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_employee_pinned_swap_shift_swap_request_shift_swap_id",
                    column: item => item.shift_swap_id,
                    principalTable: "shift_swap_request",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_employee_pinned_swap_shift_swap_id",
            table: "employee_pinned_swap",
            column: "shift_swap_id");
        migrationBuilder.CreateIndex(
            name: "ix_employee_pinned_swap_employee_time",
            table: "employee_pinned_swap",
            columns: new[] { "employee_id", "pinned_at_utc" });
        migrationBuilder.CreateIndex(
            name: "ux_employee_pinned_swap_employee_swap",
            table: "employee_pinned_swap",
            columns: new[] { "employee_id", "shift_swap_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "employee_pinned_swap");
    }
}
