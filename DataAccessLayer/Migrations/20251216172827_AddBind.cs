using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddBind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shift_no",
                table: "schedule_slot");

            migrationBuilder.CreateTable(
                name: "AvailabilityBinds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityBinds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityBinds_Key",
                table: "AvailabilityBinds",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityBinds");

            migrationBuilder.AddColumn<int>(
                name: "shift_no",
                table: "schedule_slot",
                type: "INTEGER",
                nullable: true);
        }
    }
}
