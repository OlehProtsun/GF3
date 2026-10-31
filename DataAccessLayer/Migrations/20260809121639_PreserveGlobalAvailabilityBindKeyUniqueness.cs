using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class PreserveGlobalAvailabilityBindKeyUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_availability_bind_global_key",
                table: "AvailabilityBinds",
                column: "Key",
                unique: true,
                filter: "\"ManagerAccountId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_availability_bind_global_key",
                table: "AvailabilityBinds");
        }
    }
}
