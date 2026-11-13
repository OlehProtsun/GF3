using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260716120000_ProtectBootstrapManager")]
    public partial class ProtectBootstrapManager : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "manager_account",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE manager_account
                SET is_system = 1
                WHERE id = (
                    SELECT id
                    FROM manager_account
                    ORDER BY id
                    LIMIT 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "ux_manager_account_system",
                table: "manager_account",
                column: "is_system",
                unique: true,
                filter: "\"is_system\" = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_manager_account_system",
                table: "manager_account");

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "manager_account");
        }
    }
}
