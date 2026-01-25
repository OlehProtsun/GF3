using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class AddScheduleCellStyles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_cell_style",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    background_color_argb = table.Column<int>(type: "INTEGER", nullable: true),
                    text_color_argb = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_cell_style", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_cell_style_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_cell_style_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_cell_style_employee_id",
                table: "schedule_cell_style",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_cell_style_schedule_id",
                table: "schedule_cell_style",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_cell_style_schedule_id_day_of_month_employee_id",
                table: "schedule_cell_style",
                columns: new[] { "schedule_id", "day_of_month", "employee_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_schedule_cell_style_dom",
                table: "schedule_cell_style",
                sql: "day_of_month BETWEEN 1 AND 31");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_cell_style");
        }
    }
}
