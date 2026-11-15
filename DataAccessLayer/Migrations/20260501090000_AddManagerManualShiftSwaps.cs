using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260501090000_AddManagerManualShiftSwaps")]
    public partial class AddManagerManualShiftSwaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                PRAGMA foreign_keys=OFF;

                CREATE TABLE "__temp_shift_swap_request" (
                    "id" INTEGER NOT NULL CONSTRAINT "PK_shift_swap_request" PRIMARY KEY AUTOINCREMENT,
                    "schedule_id" INTEGER NOT NULL,
                    "schedule_slot_id" INTEGER NOT NULL,
                    "offered_from_time" TEXT NULL,
                    "offered_to_time" TEXT NULL,
                    "from_employee_id" INTEGER NULL,
                    "target_employee_id" INTEGER NULL,
                    "accepted_by_employee_id" INTEGER NULL,
                    "visibility" TEXT NOT NULL DEFAULT 'Public',
                    "status" TEXT NOT NULL DEFAULT 'Open',
                    "created_at_utc" TEXT NOT NULL,
                    "accepted_at_utc" TEXT NULL,
                    "cancelled_at_utc" TEXT NULL,
                    "is_manager_created" INTEGER NOT NULL DEFAULT 0,
                    "manual_column_id" INTEGER NULL,
                    CONSTRAINT "FK_shift_swap_request_employee_accepted_by_employee_id" FOREIGN KEY ("accepted_by_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_employee_from_employee_id" FOREIGN KEY ("from_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_employee_target_employee_id" FOREIGN KEY ("target_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_schedule_schedule_id" FOREIGN KEY ("schedule_id") REFERENCES "schedule" ("id") ON DELETE CASCADE,
                    CONSTRAINT "FK_shift_swap_request_schedule_slot_schedule_slot_id" FOREIGN KEY ("schedule_slot_id") REFERENCES "schedule_slot" ("id") ON DELETE CASCADE
                );

                INSERT INTO "__temp_shift_swap_request" (
                    "id",
                    "schedule_id",
                    "schedule_slot_id",
                    "offered_from_time",
                    "offered_to_time",
                    "from_employee_id",
                    "target_employee_id",
                    "accepted_by_employee_id",
                    "visibility",
                    "status",
                    "created_at_utc",
                    "accepted_at_utc",
                    "cancelled_at_utc",
                    "is_manager_created",
                    "manual_column_id"
                )
                SELECT
                    "id",
                    "schedule_id",
                    "schedule_slot_id",
                    "offered_from_time",
                    "offered_to_time",
                    "from_employee_id",
                    "target_employee_id",
                    "accepted_by_employee_id",
                    "visibility",
                    "status",
                    "created_at_utc",
                    "accepted_at_utc",
                    "cancelled_at_utc",
                    0,
                    NULL
                FROM "shift_swap_request";

                DROP TABLE "shift_swap_request";
                ALTER TABLE "__temp_shift_swap_request" RENAME TO "shift_swap_request";

                CREATE INDEX "IX_shift_swap_request_accepted_by_employee_id" ON "shift_swap_request" ("accepted_by_employee_id");
                CREATE INDEX "ix_shift_swap_from_status" ON "shift_swap_request" ("from_employee_id", "status");
                CREATE INDEX "ix_shift_swap_schedule_status" ON "shift_swap_request" ("schedule_id", "status");
                CREATE INDEX "ix_shift_swap_target_status" ON "shift_swap_request" ("target_employee_id", "status");
                CREATE UNIQUE INDEX "ux_shift_swap_open_slot" ON "shift_swap_request" ("schedule_slot_id", "status") WHERE status = 'Open';

                PRAGMA foreign_keys=ON;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                PRAGMA foreign_keys=OFF;

                CREATE TABLE "__temp_shift_swap_request" (
                    "id" INTEGER NOT NULL CONSTRAINT "PK_shift_swap_request" PRIMARY KEY AUTOINCREMENT,
                    "schedule_id" INTEGER NOT NULL,
                    "schedule_slot_id" INTEGER NOT NULL,
                    "offered_from_time" TEXT NULL,
                    "offered_to_time" TEXT NULL,
                    "from_employee_id" INTEGER NOT NULL,
                    "target_employee_id" INTEGER NULL,
                    "accepted_by_employee_id" INTEGER NULL,
                    "visibility" TEXT NOT NULL DEFAULT 'Public',
                    "status" TEXT NOT NULL DEFAULT 'Open',
                    "created_at_utc" TEXT NOT NULL,
                    "accepted_at_utc" TEXT NULL,
                    "cancelled_at_utc" TEXT NULL,
                    CONSTRAINT "FK_shift_swap_request_employee_accepted_by_employee_id" FOREIGN KEY ("accepted_by_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_employee_from_employee_id" FOREIGN KEY ("from_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_employee_target_employee_id" FOREIGN KEY ("target_employee_id") REFERENCES "employee" ("id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_shift_swap_request_schedule_schedule_id" FOREIGN KEY ("schedule_id") REFERENCES "schedule" ("id") ON DELETE CASCADE,
                    CONSTRAINT "FK_shift_swap_request_schedule_slot_schedule_slot_id" FOREIGN KEY ("schedule_slot_id") REFERENCES "schedule_slot" ("id") ON DELETE CASCADE
                );

                INSERT INTO "__temp_shift_swap_request" (
                    "id",
                    "schedule_id",
                    "schedule_slot_id",
                    "offered_from_time",
                    "offered_to_time",
                    "from_employee_id",
                    "target_employee_id",
                    "accepted_by_employee_id",
                    "visibility",
                    "status",
                    "created_at_utc",
                    "accepted_at_utc",
                    "cancelled_at_utc"
                )
                SELECT
                    "id",
                    "schedule_id",
                    "schedule_slot_id",
                    "offered_from_time",
                    "offered_to_time",
                    "from_employee_id",
                    "target_employee_id",
                    "accepted_by_employee_id",
                    "visibility",
                    "status",
                    "created_at_utc",
                    "accepted_at_utc",
                    "cancelled_at_utc"
                FROM "shift_swap_request"
                WHERE "from_employee_id" IS NOT NULL;

                DROP TABLE "shift_swap_request";
                ALTER TABLE "__temp_shift_swap_request" RENAME TO "shift_swap_request";

                CREATE INDEX "IX_shift_swap_request_accepted_by_employee_id" ON "shift_swap_request" ("accepted_by_employee_id");
                CREATE INDEX "ix_shift_swap_from_status" ON "shift_swap_request" ("from_employee_id", "status");
                CREATE INDEX "ix_shift_swap_schedule_status" ON "shift_swap_request" ("schedule_id", "status");
                CREATE INDEX "ix_shift_swap_target_status" ON "shift_swap_request" ("target_employee_id", "status");
                CREATE UNIQUE INDEX "ux_shift_swap_open_slot" ON "shift_swap_request" ("schedule_slot_id", "status") WHERE status = 'Open';

                PRAGMA foreign_keys=ON;
                """);
        }
    }
}
