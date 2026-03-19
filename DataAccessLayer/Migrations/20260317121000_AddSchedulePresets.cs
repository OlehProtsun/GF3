using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulePresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_preset",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    container_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    schedule_name = table.Column<string>(type: "TEXT", nullable: false),
                    shop_id = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    people_per_shift = table.Column<int>(type: "INTEGER", nullable: false),
                    shift1_time = table.Column<string>(type: "TEXT", nullable: false),
                    shift2_time = table.Column<string>(type: "TEXT", nullable: false),
                    max_hours_per_emp_month = table.Column<int>(type: "INTEGER", nullable: false),
                    max_consecutive_days = table.Column<int>(type: "INTEGER", nullable: false),
                    max_consecutive_full = table.Column<int>(type: "INTEGER", nullable: false),
                    max_full_per_month = table.Column<int>(type: "INTEGER", nullable: false),
                    availability_group_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_preset", x => x.id);

                    table.CheckConstraint(
                        name: "ck_schedule_preset_month",
                        sql: "month BETWEEN 1 AND 12");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_people_per_shift",
                        sql: "people_per_shift >= 1");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_max_hours_per_emp_month",
                        sql: "max_hours_per_emp_month >= 1");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_max_consecutive_days",
                        sql: "max_consecutive_days >= 1");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_max_consecutive_full",
                        sql: "max_consecutive_full >= 1");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_max_full_per_month",
                        sql: "max_full_per_month >= 1");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_shift1_format",
                        sql: "shift1_time LIKE '__:__ - __:__'");

                    table.CheckConstraint(
                        name: "ck_schedule_preset_shift2_format",
                        sql: "shift2_time LIKE '__:__ - __:__'");

                    table.ForeignKey(
                        name: "FK_schedule_preset_availability_group_availability_group_id",
                        column: x => x.availability_group_id,
                        principalTable: "availability_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);

                    table.ForeignKey(
                        name: "FK_schedule_preset_container_container_id",
                        column: x => x.container_id,
                        principalTable: "container",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_schedule_preset_shop_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schedule_preset_employee",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_preset_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    min_hours_month = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_preset_employee", x => x.id);

                    table.CheckConstraint(
                        name: "ck_schedule_preset_employee_min_hours",
                        sql: "min_hours_month >= 0");

                    table.ForeignKey(
                        name: "FK_schedule_preset_employee_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_schedule_preset_employee_schedule_preset_schedule_preset_id",
                        column: x => x.schedule_preset_id,
                        principalTable: "schedule_preset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_preset_avail_group",
                table: "schedule_preset",
                column: "availability_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_preset_container",
                table: "schedule_preset",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_preset_shop",
                table: "schedule_preset",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "ux_schedule_preset_container_name",
                table: "schedule_preset",
                columns: new[] { "container_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schedule_preset_employee_employee",
                table: "schedule_preset_employee",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ux_schedule_preset_employee",
                table: "schedule_preset_employee",
                columns: new[] { "schedule_preset_id", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_preset_employee");

            migrationBuilder.DropTable(
                name: "schedule_preset");
        }
    }
}
