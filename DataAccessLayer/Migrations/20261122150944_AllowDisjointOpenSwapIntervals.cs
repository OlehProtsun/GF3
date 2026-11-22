using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AllowDisjointOpenSwapIntervals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shift_swap_open_return_slot",
                table: "shift_swap_request");

            migrationBuilder.DropIndex(
                name: "ux_shift_swap_open_slot",
                table: "shift_swap_request");

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_open_return_slot",
                table: "shift_swap_request",
                columns: new[] { "return_schedule_slot_id", "status" },
                filter: "status = 'Open' AND request_kind = 'Exchange' AND return_schedule_slot_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_swap_open_slot",
                table: "shift_swap_request",
                columns: new[] { "schedule_slot_id", "status" },
                filter: "status = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The downgrade fails atomically if either old unique index would reject multiple open requests.
            migrationBuilder.DropIndex(
                name: "ix_shift_swap_open_return_slot",
                table: "shift_swap_request");

            migrationBuilder.DropIndex(
                name: "ix_shift_swap_open_slot",
                table: "shift_swap_request");

            migrationBuilder.CreateIndex(
                name: "ux_shift_swap_open_return_slot",
                table: "shift_swap_request",
                columns: new[] { "return_schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Open' AND request_kind = 'Exchange' AND return_schedule_slot_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_shift_swap_open_slot",
                table: "shift_swap_request",
                columns: new[] { "schedule_slot_id", "status" },
                unique: true,
                filter: "status = 'Open'");
        }
    }
}
