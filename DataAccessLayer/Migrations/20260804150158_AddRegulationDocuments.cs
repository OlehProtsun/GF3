using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddRegulationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "regulation_document",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    version = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    pdf_file_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    pdf_content = table.Column<byte[]>(type: "BLOB", nullable: false),
                    pdf_sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    is_published = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    created_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_by_manager_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulation_document", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regulation_acceptance",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    regulation_document_id = table.Column<int>(type: "INTEGER", nullable: false),
                    account_role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    username_snapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    display_name_snapshot = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    accepted_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulation_acceptance", x => x.id);
                    table.CheckConstraint("ck_regulation_acceptance_role", "account_role IN ('manager', 'employee')");
                    table.ForeignKey(
                        name: "FK_regulation_acceptance_regulation_document_regulation_document_id",
                        column: x => x.regulation_document_id,
                        principalTable: "regulation_document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_regulation_acceptance_subject_time",
                table: "regulation_acceptance",
                columns: new[] { "account_role", "account_id", "accepted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_regulation_acceptance_subject",
                table: "regulation_acceptance",
                columns: new[] { "regulation_document_id", "account_role", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_regulation_document_published",
                table: "regulation_document",
                columns: new[] { "is_published", "published_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_regulation_document_version",
                table: "regulation_document",
                column: "version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "regulation_acceptance");

            migrationBuilder.DropTable(
                name: "regulation_document");
        }
    }
}
