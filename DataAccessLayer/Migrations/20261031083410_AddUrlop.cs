using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "urlop_day",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    request_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    shop_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    reviewed_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_urlop_day", x => x.id);
                    table.CheckConstraint("ck_urlop_day_status", "status IN ('pending','approved','rejected')");
                    table.ForeignKey(
                        name: "FK_urlop_day_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_urlop_day_manager_account_created_by_manager_id",
                        column: x => x.created_by_manager_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_urlop_day_manager_account_reviewed_by_manager_id",
                        column: x => x.reviewed_by_manager_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_urlop_day_shop_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "urlop_month",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    shop_id = table.Column<int>(type: "INTEGER", nullable: false),
                    year = table.Column<int>(type: "INTEGER", nullable: false),
                    month = table.Column<int>(type: "INTEGER", nullable: false),
                    is_open = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    revision = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_urlop_month", x => x.id);
                    table.CheckConstraint("ck_urlop_month_month", "month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_urlop_month_year", "year BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "FK_urlop_month_shop_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_created_by_manager_id",
                table: "urlop_day",
                column: "created_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_employee_id_date",
                table: "urlop_day",
                columns: new[] { "employee_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_request_id",
                table: "urlop_day",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_reviewed_by_manager_id",
                table: "urlop_day",
                column: "reviewed_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_shop_id_date",
                table: "urlop_day",
                columns: new[] { "shop_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_urlop_day_shop_id_employee_id_date",
                table: "urlop_day",
                columns: new[] { "shop_id", "employee_id", "date" },
                unique: true,
                filter: "status IN ('pending','approved')");

            migrationBuilder.CreateIndex(
                name: "IX_urlop_month_shop_id_year_month",
                table: "urlop_month",
                columns: new[] { "shop_id", "year", "month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "urlop_day");

            migrationBuilder.DropTable(
                name: "urlop_month");
        }
    }
}
