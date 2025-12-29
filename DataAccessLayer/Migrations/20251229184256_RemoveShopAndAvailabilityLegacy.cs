using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShopAndAvailabilityLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedule_shop_shop_id",
                table: "schedule");

            migrationBuilder.DropTable(
                name: "availability_day");

            migrationBuilder.DropTable(
                name: "shop");

            migrationBuilder.DropTable(
                name: "availability_month");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_month",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_month", x => x.id);
                    table.CheckConstraint("ck_availability_month_month", "month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_availability_month_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "availability_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    availability_month_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    interval_str = table.Column<string>(type: "TEXT", nullable: true),
                    kind = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_day", x => x.id);
                    table.CheckConstraint("ck_availability_day_dom", "day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_availability_day_kind_interval", "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind IN ('ANY','NONE') AND interval_str IS NULL))");
                    table.ForeignKey(
                        name: "FK_availability_day_availability_month_availability_month_id",
                        column: x => x.availability_month_id,
                        principalTable: "availability_month",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avail_day_month",
                table: "availability_day",
                columns: new[] { "availability_month_id", "day_of_month" });

            migrationBuilder.CreateIndex(
                name: "ux_availability_day_month_day",
                table: "availability_day",
                columns: new[] { "availability_month_id", "day_of_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_avail_month_emp",
                table: "availability_month",
                columns: new[] { "employee_id", "year", "month" });

            migrationBuilder.CreateIndex(
                name: "ux_availability_month_emp_year_month",
                table: "availability_month",
                columns: new[] { "employee_id", "year", "month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_shop_shop_id",
                table: "schedule",
                column: "shop_id",
                principalTable: "shop",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
