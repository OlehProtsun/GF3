using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260318143000_AddAvailabilityGroupMemberDisplayOrder")]
    public partial class AddAvailabilityGroupMemberDisplayOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "availability_group_member",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                WITH ordered_members AS (
                    SELECT
                        agm.id,
                        ROW_NUMBER() OVER (
                            PARTITION BY agm.availability_group_id
                            ORDER BY
                                COALESCE(e.first_name, ''),
                                COALESCE(e.last_name, ''),
                                agm.employee_id,
                                agm.id
                        ) - 1 AS display_order
                    FROM availability_group_member AS agm
                    LEFT JOIN employee AS e ON e.id = agm.employee_id
                )
                UPDATE availability_group_member
                SET display_order = (
                    SELECT ordered_members.display_order
                    FROM ordered_members
                    WHERE ordered_members.id = availability_group_member.id
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_order",
                table: "availability_group_member");
        }
    }
}
