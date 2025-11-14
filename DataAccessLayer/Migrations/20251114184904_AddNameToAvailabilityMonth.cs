using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToAvailabilityMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "container",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_container", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    first_name = table.Column<string>(type: "TEXT", nullable: false),
                    last_name = table.Column<string>(type: "TEXT", nullable: false),
                    phone = table.Column<string>(type: "TEXT", nullable: true),
                    email = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shop",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "availability_month",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    container_id = table.Column<int>(type: "INTEGER", nullable: false),
                    shop_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    people_per_shift = table.Column<int>(type: "INTEGER", nullable: false),
                    shift1_time = table.Column<string>(type: "TEXT", nullable: false),
                    shift2_time = table.Column<string>(type: "TEXT", nullable: false),
                    max_hours_per_emp_month = table.Column<int>(type: "INTEGER", nullable: false),
                    max_consecutive_days = table.Column<int>(type: "INTEGER", nullable: false),
                    max_consecutive_full = table.Column<int>(type: "INTEGER", nullable: false),
                    max_full_per_month = table.Column<int>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule", x => x.id);
                    table.CheckConstraint("ck_schedule_max_consecutive_days", "max_consecutive_days >= 1");
                    table.CheckConstraint("ck_schedule_max_consecutive_full", "max_consecutive_full >= 1");
                    table.CheckConstraint("ck_schedule_max_full_per_month", "max_full_per_month >= 0");
                    table.CheckConstraint("ck_schedule_max_hours_per_emp_month", "max_hours_per_emp_month >= 0");
                    table.CheckConstraint("ck_schedule_month", "month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_schedule_people_per_shift", "people_per_shift >= 1");
                    table.CheckConstraint("ck_schedule_shift1_format", "shift1_time LIKE '__:__ - __:__'");
                    table.CheckConstraint("ck_schedule_shift2_format", "shift2_time LIKE '__:__ - __:__'");
                    table.ForeignKey(
                        name: "FK_schedule_container_container_id",
                        column: x => x.container_id,
                        principalTable: "container",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schedule_shop_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    availability_month_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", nullable: false),
                    interval_str = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "schedule_employee",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    min_hours_month = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_employee", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_employee_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_employee_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_slot",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    schedule_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    shift_no = table.Column<int>(type: "INTEGER", nullable: false),
                    slot_no = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "UNFURNISHED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_slot", x => x.id);
                    table.CheckConstraint("ck_schedule_slot_dom", "day_of_month BETWEEN 1 AND 31");
                    table.CheckConstraint("ck_schedule_slot_shift_no", "shift_no IN (1,2)");
                    table.CheckConstraint("ck_schedule_slot_slot_no", "slot_no >= 1");
                    table.CheckConstraint("ck_schedule_slot_status_pair", "((status='UNFURNISHED' AND employee_id IS NULL) OR (status='ASSIGNED' AND employee_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_schedule_slot_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_schedule_slot_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "schedule",
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

            migrationBuilder.CreateIndex(
                name: "IX_container_name",
                table: "container",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sched_container",
                table: "schedule",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_sched_container_shop",
                table: "schedule",
                columns: new[] { "container_id", "shop_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sched_shop_month",
                table: "schedule",
                columns: new[] { "shop_id", "year", "month" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_employee_employee_id",
                table: "schedule_employee",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_employee_schedule_id_employee_id",
                table: "schedule_employee",
                columns: new[] { "schedule_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slot_employee_id",
                table: "schedule_slot",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slot_schedule_id_day_of_month_shift_no_slot_no",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "slot_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_slot_sched_day_shift",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no" });

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_shift",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "employee_id" },
                unique: true,
                filter: "employee_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_slot_unique_emp_per_shift1",
                table: "schedule_slot",
                columns: new[] { "schedule_id", "day_of_month", "shift_no", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_day");

            migrationBuilder.DropTable(
                name: "schedule_employee");

            migrationBuilder.DropTable(
                name: "schedule_slot");

            migrationBuilder.DropTable(
                name: "availability_month");

            migrationBuilder.DropTable(
                name: "schedule");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "container");

            migrationBuilder.DropTable(
                name: "shop");
        }
    }
}
