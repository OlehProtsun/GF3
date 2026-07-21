using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260721130000_AddSystemNews")]
public sealed class AddSystemNews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "system_news_message",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                body = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                audience = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                image_url = table.Column<string>(type: "TEXT", maxLength: 2800000, nullable: true),
                video_url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_system_news_message", item => item.id));

        migrationBuilder.CreateTable(
            name: "system_news_read",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                message_id = table.Column<int>(type: "INTEGER", nullable: false),
                account_role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                account_id = table.Column<int>(type: "INTEGER", nullable: false),
                read_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_system_news_read", item => item.id);
                table.ForeignKey(
                    name: "FK_system_news_read_system_news_message_message_id",
                    column: item => item.message_id,
                    principalTable: "system_news_message",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("ix_system_news_created", "system_news_message", "created_at_utc");
        migrationBuilder.CreateIndex(
            "ux_system_news_read_subject",
            "system_news_read",
            new[] { "message_id", "account_role", "account_id" },
            unique: true);
        migrationBuilder.CreateIndex(
            "ix_system_news_read_subject_time",
            "system_news_read",
            new[] { "account_role", "account_id", "read_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("system_news_read");
        migrationBuilder.DropTable("system_news_message");
    }
}
