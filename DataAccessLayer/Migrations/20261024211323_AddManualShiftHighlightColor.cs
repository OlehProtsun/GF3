using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddManualShiftHighlightColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accepted_manual_shift_highlight_color",
                table: "schedule",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "#BAE6FD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted_manual_shift_highlight_color",
                table: "schedule");
        }
    }
}
