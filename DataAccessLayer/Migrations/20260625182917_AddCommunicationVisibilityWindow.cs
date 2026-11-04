using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationVisibilityWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "visible_from_utc",
                table: "communication_message",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_communication_message_visible_from",
                table: "communication_message",
                column: "visible_from_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_communication_message_visible_from",
                table: "communication_message");

            migrationBuilder.DropColumn(
                name: "visible_from_utc",
                table: "communication_message");
        }
    }
}
