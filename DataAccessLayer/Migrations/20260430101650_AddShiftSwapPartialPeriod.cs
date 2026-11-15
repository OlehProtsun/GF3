using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSwapPartialPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "offered_from_time",
                table: "shift_swap_request",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "offered_to_time",
                table: "shift_swap_request",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "offered_from_time",
                table: "shift_swap_request");

            migrationBuilder.DropColumn(
                name: "offered_to_time",
                table: "shift_swap_request");
        }
    }
}
