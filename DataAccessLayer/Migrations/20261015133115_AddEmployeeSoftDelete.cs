using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_employee_full_name",
                table: "employee");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "employee",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ux_employee_full_name",
                table: "employee",
                columns: new[] { "first_name", "last_name" },
                unique: true,
                filter: "\"is_deleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_employee_full_name",
                table: "employee");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "employee");

            migrationBuilder.CreateIndex(
                name: "ux_employee_full_name",
                table: "employee",
                columns: new[] { "first_name", "last_name" },
                unique: true);
        }
    }
}
