using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "manager_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "employee_account",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "manager_account");

            migrationBuilder.DropColumn(
                name: "language",
                table: "employee_account");
        }
    }
}

