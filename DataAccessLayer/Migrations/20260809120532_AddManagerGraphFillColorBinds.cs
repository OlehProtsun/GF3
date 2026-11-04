using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerGraphFillColorBinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AvailabilityBinds_Key",
                table: "AvailabilityBinds");

            migrationBuilder.AddColumn<int>(
                name: "ManagerAccountId",
                table: "AvailabilityBinds",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO AvailabilityBinds (Value, Key, IsActive, ManagerAccountId)
                SELECT bind.Value, bind.Key, bind.IsActive, manager.id
                FROM AvailabilityBinds AS bind
                CROSS JOIN manager_account AS manager
                WHERE bind.ManagerAccountId IS NULL;

                DELETE FROM AvailabilityBinds
                WHERE ManagerAccountId IS NULL
                  AND EXISTS (SELECT 1 FROM manager_account);
                """);

            migrationBuilder.CreateTable(
                name: "manager_graph_fill_color_bind",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    manager_account_id = table.Column<int>(type: "INTEGER", nullable: false),
                    key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    fill_color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manager_graph_fill_color_bind", x => x.id);
                    table.ForeignKey(
                        name: "FK_manager_graph_fill_color_bind_manager_account_manager_account_id",
                        column: x => x.manager_account_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_availability_bind_manager_key",
                table: "AvailabilityBinds",
                columns: new[] { "ManagerAccountId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_manager_graph_fill_bind_color",
                table: "manager_graph_fill_color_bind",
                columns: new[] { "manager_account_id", "fill_color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_manager_graph_fill_bind_key",
                table: "manager_graph_fill_color_bind",
                columns: new[] { "manager_account_id", "key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AvailabilityBinds_manager_account_ManagerAccountId",
                table: "AvailabilityBinds",
                column: "ManagerAccountId",
                principalTable: "manager_account",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailabilityBinds_manager_account_ManagerAccountId",
                table: "AvailabilityBinds");

            migrationBuilder.DropTable(
                name: "manager_graph_fill_color_bind");

            migrationBuilder.DropIndex(
                name: "ux_availability_bind_manager_key",
                table: "AvailabilityBinds");

            migrationBuilder.Sql(
                """
                DELETE FROM AvailabilityBinds
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM AvailabilityBinds
                    GROUP BY Key
                );

                UPDATE AvailabilityBinds SET ManagerAccountId = NULL;
                """);

            migrationBuilder.DropColumn(
                name: "ManagerAccountId",
                table: "AvailabilityBinds");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityBinds_Key",
                table: "AvailabilityBinds",
                column: "Key",
                unique: true);
        }
    }
}
