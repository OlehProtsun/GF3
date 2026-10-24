using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class TrackAvailabilityNotificationEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "joined_at_utc",
                table: "availability_group_member",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "notification_available_at_utc",
                table: "availability_group",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at_utc",
                table: "availability_group",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "notification_legacy_visible_from_utc",
                table: "availability_group",
                type: "TEXT",
                nullable: true);
            migrationBuilder.Sql("UPDATE availability_group SET notification_legacy_visible_from_utc = visible_from_utc;");
            // Only future visibility starts are known events. Do not invent publication times
            // or replay already visible legacy forms at deployment.
            migrationBuilder.Sql("""
                UPDATE availability_group SET notification_available_at_utc = visible_from_utc
                WHERE julianday(visible_from_utc) > julianday('now');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "notification_legacy_visible_from_utc", table: "availability_group");
            migrationBuilder.DropColumn(
                name: "joined_at_utc",
                table: "availability_group_member");

            migrationBuilder.DropColumn(
                name: "notification_available_at_utc",
                table: "availability_group");

            migrationBuilder.DropColumn(
                name: "published_at_utc",
                table: "availability_group");
        }
    }
}
