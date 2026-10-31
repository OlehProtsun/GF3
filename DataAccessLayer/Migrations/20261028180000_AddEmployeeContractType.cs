using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeContractType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contract_type",
                table: "employee",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "zlecenie");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_contract_type",
                table: "employee",
                sql: "contract_type IN ('zlecenie','o_prace')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_contract_type",
                table: "employee");

            migrationBuilder.DropColumn(
                name: "contract_type",
                table: "employee");
        }
    }
}
