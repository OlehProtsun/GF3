using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

public partial class AddCommunicationContentBlocks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "content_blocks_json",
            table: "communication_message",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "content_blocks_json",
            table: "communication_message");
    }
}
