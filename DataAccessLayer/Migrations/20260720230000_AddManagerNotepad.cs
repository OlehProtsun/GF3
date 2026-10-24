using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260720230000_AddManagerNotepad")]
public partial class AddManagerNotepad : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "manager_note",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                content = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                color = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note", item => item.id);
                table.ForeignKey(
                    name: "FK_manager_note_manager_account_manager_account_id",
                    column: item => item.manager_account_id,
                    principalTable: "manager_account",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "manager_notepad_state",
            columns: table => new
            {
                manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                is_expanded = table.Column<bool>(type: "INTEGER", nullable: false),
                is_pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                height = table.Column<int>(type: "INTEGER", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_notepad_state", item => item.manager_account_id);
                table.ForeignKey(
                    name: "FK_manager_notepad_state_manager_account_manager_account_id",
                    column: item => item.manager_account_id,
                    principalTable: "manager_account",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_manager_note_manager_updated",
            table: "manager_note",
            columns: new[] { "manager_account_id", "updated_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "manager_note");
        migrationBuilder.DropTable(name: "manager_notepad_state");
    }
}
