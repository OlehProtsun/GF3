using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_priority",
                table: "schedule_employee",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "priority_generation_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    shift_hours = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 8),
                    weekends_off = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priority_generation_settings", x => x.id);
                    table.CheckConstraint("CK_priority_generation_settings_shift_hours", "shift_hours BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_priority_generation_settings_singleton", "id = 1");
                });

            migrationBuilder.InsertData(
                table: "priority_generation_settings",
                columns: new[] { "id", "shift_hours", "weekends_off" },
                values: new object[] { 1, 8, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "priority_generation_settings");

            migrationBuilder.DropColumn(
                name: "is_priority",
                table: "schedule_employee");
        }
    }
}
