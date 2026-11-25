using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

public partial class AddCommunicationPhotoAttachment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "photo_attachment_json",
            table: "communication_message",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "photo_attachment_json",
            table: "communication_message");
    }
}
