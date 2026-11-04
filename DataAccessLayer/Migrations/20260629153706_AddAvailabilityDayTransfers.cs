using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityDayTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_group_day_transfer",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    source_member_id = table.Column<int>(type: "INTEGER", nullable: false),
                    target_member_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_month = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_group_day_transfer", x => x.id);
                    table.CheckConstraint("ck_avail_transfer_distinct_members", "source_member_id <> target_member_id");
                    table.CheckConstraint("ck_avail_transfer_dom", "day_of_month BETWEEN 1 AND 31");
                    table.ForeignKey(
                        name: "FK_availability_group_day_transfer_availability_group_member_source_member_id",
                        column: x => x.source_member_id,
                        principalTable: "availability_group_member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_availability_group_day_transfer_availability_group_member_target_member_id",
                        column: x => x.target_member_id,
                        principalTable: "availability_group_member",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_avail_transfer_source_day",
                table: "availability_group_day_transfer",
                columns: new[] { "source_member_id", "day_of_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_avail_transfer_target_day",
                table: "availability_group_day_transfer",
                columns: new[] { "target_member_id", "day_of_month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_group_day_transfer");
        }
    }
}
