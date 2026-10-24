using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerGraphTextColorBinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manager_graph_text_color_bind",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    text_color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manager_graph_text_color_bind", x => x.id);
                    table.ForeignKey(
                        name: "FK_manager_graph_text_color_bind_manager_account_manager_account_id",
                        column: x => x.manager_account_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_manager_graph_text_bind_color",
                table: "manager_graph_text_color_bind",
                columns: new[] { "manager_account_id", "text_color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_manager_graph_text_bind_key",
                table: "manager_graph_text_color_bind",
                columns: new[] { "manager_account_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manager_graph_text_color_bind");
        }
    }
}
