using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_message",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    deadline_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by_manager_id = table.Column<int>(type: "INTEGER", nullable: true),
                    created_by_manager_name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_message", x => x.id);
                    table.CheckConstraint("ck_communication_message_body", "length(trim(body)) > 0");
                    table.CheckConstraint("ck_communication_message_deadline", "deadline_at_utc > created_at_utc");
                    table.CheckConstraint("ck_communication_message_title", "length(trim(title)) > 0");
                    table.ForeignKey(
                        name: "FK_communication_message_manager_account_created_by_manager_id",
                        column: x => x.created_by_manager_id,
                        principalTable: "manager_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "employee_communication_dismissal",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    communication_message_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    dismissed_at_utc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_communication_dismissal", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_communication_dismissal_communication_message_communication_message_id",
                        column: x => x.communication_message_id,
                        principalTable: "communication_message",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_communication_dismissal_employee_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_communication_message_created",
                table: "communication_message",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_communication_message_created_by_manager_id",
                table: "communication_message",
                column: "created_by_manager_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_message_deadline",
                table: "communication_message",
                column: "deadline_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_employee_comm_dismissal_emp_time",
                table: "employee_communication_dismissal",
                columns: new[] { "employee_id", "dismissed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_employee_comm_dismissal_msg_emp",
                table: "employee_communication_dismissal",
                columns: new[] { "communication_message_id", "employee_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_communication_dismissal");

            migrationBuilder.DropTable(
                name: "communication_message");
        }
    }
}
