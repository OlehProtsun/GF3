using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowLogManagementSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_log_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    audience = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false, defaultValue: "all")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_log_settings", x => x.id);
                    table.CheckConstraint("ck_workflow_log_settings_audience", "audience IN ('all', 'managers', 'employees')");
                    table.CheckConstraint("ck_workflow_log_settings_singleton", "id = 1");
                });

            migrationBuilder.InsertData(
                table: "workflow_log_settings",
                columns: new[] { "id", "audience", "is_enabled" },
                values: new object[] { 1, "all", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_log_settings");
        }
    }
}
