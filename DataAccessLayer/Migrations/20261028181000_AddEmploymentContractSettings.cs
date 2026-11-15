using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentContractSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employment_contract_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    restrict_swaps_to_eight_hours = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    require_pickup_approval = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    require_below_minimum_give_approval = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    automatic_weekday_availability = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employment_contract_settings", x => x.id);
                    table.CheckConstraint("ck_employment_contract_settings_singleton", "id = 1");
                });

            migrationBuilder.InsertData(
                table: "employment_contract_settings",
                columns: new[] { "id", "automatic_weekday_availability", "require_below_minimum_give_approval", "require_pickup_approval", "restrict_swaps_to_eight_hours" },
                values: new object[] { 1, true, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employment_contract_settings");
        }
    }
}
