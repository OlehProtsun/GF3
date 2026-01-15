using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_employee_full_name",
                table: "employee",
                columns: new[] { "first_name", "last_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_shop_name",
                table: "shop",
                column: "name",
                unique: true);

            migrationBuilder.DropIndex(
                name: "ix_avail_group_year_month_name",
                table: "availability_group");

            migrationBuilder.CreateIndex(
                name: "ux_avail_group_year_month_name",
                table: "availability_group",
                columns: new[] { "year", "month", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_employee_full_name",
                table: "employee");

            migrationBuilder.DropIndex(
                name: "ux_shop_name",
                table: "shop");

            migrationBuilder.DropIndex(
                name: "ux_avail_group_year_month_name",
                table: "availability_group");

            migrationBuilder.CreateIndex(
                name: "ix_avail_group_year_month_name",
                table: "availability_group",
                columns: new[] { "year", "month", "name" });
        }
    }
}
