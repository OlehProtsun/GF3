using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAvailabilityGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "availability_group_id",
                table: "schedule",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sched_avail_group",
                table: "schedule",
                column: "availability_group_id");

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_availability_group_availability_group_id",
                table: "schedule",
                column: "availability_group_id",
                principalTable: "availability_group",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedule_availability_group_availability_group_id",
                table: "schedule");

            migrationBuilder.DropIndex(
                name: "ix_sched_avail_group",
                table: "schedule");

            migrationBuilder.DropColumn(
                name: "availability_group_id",
                table: "schedule");
        }
    }
}
