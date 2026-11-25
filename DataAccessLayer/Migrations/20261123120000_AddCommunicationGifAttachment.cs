using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

public partial class AddCommunicationGifAttachment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "gif_attachment_json",
            table: "communication_message",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "gif_attachment_json",
            table: "communication_message");
    }
}
