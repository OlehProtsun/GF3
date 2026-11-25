using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

public partial class AddWorkflowLogLaunchMode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(
            name: "launch_mode",
            table: "workflow_log_entry",
            type: "TEXT",
            maxLength: 7,
            nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "launch_mode", table: "workflow_log_entry");
}
