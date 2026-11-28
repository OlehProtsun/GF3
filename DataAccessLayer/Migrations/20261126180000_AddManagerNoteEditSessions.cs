using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataAccessLayer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261126180000_AddManagerNoteEditSessions")]
public sealed class AddManagerNoteEditSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("edit_session_id", "manager_note", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<int>("edit_manager_id", "manager_note", type: "INTEGER", nullable: true);
        migrationBuilder.AddColumn<long>("edit_started_at_ms", "manager_note", type: "INTEGER", nullable: true);
        migrationBuilder.AddColumn<long>("edit_expires_at_ms", "manager_note", type: "INTEGER", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN edit_expires_at_ms");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN edit_started_at_ms");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN edit_manager_id");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN edit_session_id");
    }
}
