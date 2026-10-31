using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWebPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_push_subscription",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    endpoint = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    p256dh = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    auth = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    updated_at_utc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_push_subscription", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_push_subscription_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_push_delivery",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    subscription_id = table.Column<int>(type: "INTEGER", nullable: false),
                    notification_id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    next_attempt_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                    lease_until_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    lease_token = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    finalized_at_utc = table.Column<long>(type: "INTEGER", nullable: true),
                    outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_push_delivery", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_push_delivery_employee_push_subscription_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "employee_push_subscription",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_push_delivery_finalized_at_utc",
                table: "employee_push_delivery",
                column: "finalized_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_employee_push_delivery_next_attempt_at_utc_lease_until_utc",
                table: "employee_push_delivery",
                columns: new[] { "next_attempt_at_utc", "lease_until_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_push_delivery_subscription_id_notification_id",
                table: "employee_push_delivery",
                columns: new[] { "subscription_id", "notification_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_push_subscription_employee_id_id",
                table: "employee_push_subscription",
                columns: new[] { "employee_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_push_subscription_endpoint",
                table: "employee_push_subscription",
                column: "endpoint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_push_delivery");

            migrationBuilder.DropTable(
                name: "employee_push_subscription");
        }
    }
}
