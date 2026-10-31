using System;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508113000_AddManagerAccounts")]
    /// <inheritdoc />
    public partial class AddManagerAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manager_account",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: false),
                    password_updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    last_login_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manager_account", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_manager_account_username",
                table: "manager_account",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manager_account");
        }
    }
}
