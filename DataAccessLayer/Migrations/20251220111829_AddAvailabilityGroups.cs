using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_group",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_group", x => x.id);
                    table.CheckConstraint("ck_availability_group_month", "month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "availability_group_member",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    availability_group_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_group_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_availability_group_member_availability_group_availability_group_id",
                        column: x => x.availability_group_id,
                        principalTable: "availability_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_availability_group_member_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_group_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    availability_group_member_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", nullable: false),
                    interval_str = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_group_day", x => x.id);
                    table.CheckConstraint("ck_avail_group_day_dom", "day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_avail_group_day_kind_interval", "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind IN ('ANY','NONE') AND interval_str IS NULL))");
                    table.ForeignKey(
                        name: "FK_availability_group_day_availability_group_member_availability_group_member_id",
                        column: x => x.availability_group_member_id,
                        principalTable: "availability_group_member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avail_group_year_month_name",
                table: "availability_group",
                columns: new[] { "year", "month", "name" });

            migrationBuilder.CreateIndex(
                name: "ux_avail_group_day_member_dom",
                table: "availability_group_day",
                columns: new[] { "availability_group_member_id", "day_of_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_availability_group_member_employee_id",
                table: "availability_group_member",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ux_avail_group_member_group_emp",
                table: "availability_group_member",
                columns: new[] { "availability_group_id", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_group_day");

            migrationBuilder.DropTable(
                name: "availability_group_member");

            migrationBuilder.DropTable(
                name: "availability_group");
        }
    }
}
