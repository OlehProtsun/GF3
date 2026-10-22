using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class DefaultPolishAccountLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "manager_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "pl",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "employee_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "pl",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "manager_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2,
                oldDefaultValue: "pl");

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "employee_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2,
                oldDefaultValue: "pl");
        }
    }
}
