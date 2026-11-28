using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261126170000_ExpandManagerNotepad")]
public partial class ExpandManagerNotepad : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_locked",
            table: "manager_note",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "is_pinned",
            table: "manager_note",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "last_edited_by_manager_id",
            table: "manager_note",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "revision",
            table: "manager_note",
            type: "INTEGER",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<string>(
            name: "visibility",
            table: "manager_note",
            type: "TEXT",
            maxLength: 7,
            nullable: false,
            defaultValue: "private");

        migrationBuilder.CreateTable(
            name: "manager_note_folder",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note_folder", x => x.id);
                table.ForeignKey(
                    name: "FK_manager_note_folder_manager_account_manager_account_id",
                    column: x => x.manager_account_id,
                    principalTable: "manager_account",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "manager_note_share",
            columns: table => new
            {
                note_id = table.Column<int>(type: "INTEGER", nullable: false),
                manager_account_id = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note_share", x => new { x.note_id, x.manager_account_id });
                table.ForeignKey(
                    name: "FK_manager_note_share_manager_account_manager_account_id",
                    column: x => x.manager_account_id,
                    principalTable: "manager_account",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_manager_note_share_manager_note_note_id",
                    column: x => x.note_id,
                    principalTable: "manager_note",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "manager_note_tag",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                name = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, collation: "NOCASE"),
                created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note_tag", x => x.id);
                table.ForeignKey(
                    name: "FK_manager_note_tag_manager_account_manager_account_id",
                    column: x => x.manager_account_id,
                    principalTable: "manager_account",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "manager_note_version",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                note_id = table.Column<int>(type: "INTEGER", nullable: false),
                revision = table.Column<long>(type: "INTEGER", nullable: false),
                title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                content = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                color = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                saved_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                saved_by_manager_id = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note_version", x => x.id);
                table.ForeignKey(
                    name: "FK_manager_note_version_manager_note_note_id",
                    column: x => x.note_id,
                    principalTable: "manager_note",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "manager_note_tag_link",
            columns: table => new
            {
                note_id = table.Column<int>(type: "INTEGER", nullable: false),
                tag_id = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_manager_note_tag_link", x => new { x.note_id, x.tag_id });
                table.ForeignKey(
                    name: "FK_manager_note_tag_link_manager_note_note_id",
                    column: x => x.note_id,
                    principalTable: "manager_note",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_manager_note_tag_link_manager_note_tag_tag_id",
                    column: x => x.tag_id,
                    principalTable: "manager_note_tag",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // SQLite supports adding a nullable reference without rebuilding the existing notes table.
        migrationBuilder.Sql("""
            ALTER TABLE manager_note ADD COLUMN folder_id INTEGER
                REFERENCES manager_note_folder(id) ON DELETE SET NULL
            """);

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_folder_id",
            table: "manager_note",
            column: "folder_id");

        migrationBuilder.CreateIndex(
            name: "ix_manager_note_manager_pinned_updated",
            table: "manager_note",
            columns: new[] { "manager_account_id", "is_pinned", "updated_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_manager_note_visibility_updated",
            table: "manager_note",
            columns: new[] { "visibility", "updated_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_folder_manager_account_id_name",
            table: "manager_note_folder",
            columns: new[] { "manager_account_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_share_manager_account_id",
            table: "manager_note_share",
            column: "manager_account_id");

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_tag_manager_account_id_name",
            table: "manager_note_tag",
            columns: new[] { "manager_account_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_tag_link_tag_id",
            table: "manager_note_tag_link",
            column: "tag_id");

        migrationBuilder.CreateIndex(
            name: "IX_manager_note_version_note_id_revision",
            table: "manager_note_version",
            columns: new[] { "note_id", "revision" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "manager_note_share");

        migrationBuilder.DropTable(
            name: "manager_note_tag_link");

        migrationBuilder.DropTable(
            name: "manager_note_version");

        migrationBuilder.DropTable(
            name: "manager_note_tag");

        migrationBuilder.DropIndex(
            name: "IX_manager_note_folder_id",
            table: "manager_note");

        migrationBuilder.DropIndex(
            name: "ix_manager_note_manager_pinned_updated",
            table: "manager_note");

        migrationBuilder.DropIndex(
            name: "ix_manager_note_visibility_updated",
            table: "manager_note");

        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN folder_id");
        migrationBuilder.DropTable(name: "manager_note_folder");

        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN is_locked");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN is_pinned");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN last_edited_by_manager_id");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN revision");
        migrationBuilder.Sql("ALTER TABLE manager_note DROP COLUMN visibility");
    }
}
