using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ReservePendingClaimOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE contract_shift_approval_request
SET status = 'expired', failure_reason = 'Superseded by exclusive pending pickup request.'
WHERE kind = 'claim' AND status = 'pending' AND shift_swap_request_id IS NOT NULL
  AND id NOT IN (
    SELECT MIN(id) FROM contract_shift_approval_request
    WHERE kind = 'claim' AND status = 'pending' AND shift_swap_request_id IS NOT NULL
    GROUP BY shift_swap_request_id
  );");
            migrationBuilder.CreateIndex(
                name: "ux_contract_approval_one_pending_claim_per_offer",
                table: "contract_shift_approval_request",
                column: "shift_swap_request_id",
                unique: true,
                filter: "kind='claim' AND status='pending' AND shift_swap_request_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_contract_approval_one_pending_claim_per_offer",
                table: "contract_shift_approval_request");
        }
    }
}
