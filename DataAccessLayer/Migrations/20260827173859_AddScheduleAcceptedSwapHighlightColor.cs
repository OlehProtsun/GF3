using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAcceptedSwapHighlightColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accepted_swap_highlight_color",
                table: "schedule",
                type: "TEXT",
                maxLength: 7,
                nullable: false,
                defaultValue: "#BBF7D0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted_swap_highlight_color",
                table: "schedule");
        }
    }
}
