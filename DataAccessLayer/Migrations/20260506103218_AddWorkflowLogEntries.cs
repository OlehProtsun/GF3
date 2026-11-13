using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_log_entry",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    actor_role = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    actor_employee_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actor_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_log_entry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_log_occurred_at",
                table: "workflow_log_entry",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_log_role_time",
                table: "workflow_log_entry",
                columns: new[] { "actor_role", "occurred_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_log_entry");
        }
    }
}
