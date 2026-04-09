using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260404120000_AllowAvailabilityGroupDayNotes")]
    public partial class AllowAvailabilityGroupDayNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "__temp_availability_group_day" (
                    "id" INTEGER NOT NULL CONSTRAINT "PK_availability_group_day" PRIMARY KEY AUTOINCREMENT,
                    "availability_group_member_id" INTEGER NOT NULL,
                    "day_of_month" INTEGER NOT NULL,
                    "kind" TEXT NOT NULL,
                    "interval_str" TEXT NULL,
                    CONSTRAINT "FK_availability_group_day_availability_group_member_availability_group_member_id"
                        FOREIGN KEY ("availability_group_member_id")
                        REFERENCES "availability_group_member" ("id")
                        ON DELETE CASCADE,
                    CONSTRAINT "ck_avail_group_day_dom"
                        CHECK ("day_of_month" BETWEEN 1 AND 31),
                    CONSTRAINT "ck_avail_group_day_kind_interval"
                        CHECK (
                            ("kind" = 'INT' AND "interval_str" IS NOT NULL AND length(trim("interval_str")) >= 11)
                            OR ("kind" = 'ANY' AND "interval_str" IS NULL)
                            OR "kind" = 'NONE'
                        )
                );

                INSERT INTO "__temp_availability_group_day" (
                    "id",
                    "availability_group_member_id",
                    "day_of_month",
                    "kind",
                    "interval_str"
                )
                SELECT
                    "id",
                    "availability_group_member_id",
                    "day_of_month",
                    "kind",
                    "interval_str"
                FROM "availability_group_day";

                DROP TABLE "availability_group_day";
                ALTER TABLE "__temp_availability_group_day" RENAME TO "availability_group_day";

                CREATE UNIQUE INDEX "ux_avail_group_day_member_dom"
                    ON "availability_group_day" ("availability_group_member_id", "day_of_month");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "__temp_availability_group_day" (
                    "id" INTEGER NOT NULL CONSTRAINT "PK_availability_group_day" PRIMARY KEY AUTOINCREMENT,
                    "availability_group_member_id" INTEGER NOT NULL,
                    "day_of_month" INTEGER NOT NULL,
                    "kind" TEXT NOT NULL,
                    "interval_str" TEXT NULL,
                    CONSTRAINT "FK_availability_group_day_availability_group_member_availability_group_member_id"
                        FOREIGN KEY ("availability_group_member_id")
                        REFERENCES "availability_group_member" ("id")
                        ON DELETE CASCADE,
                    CONSTRAINT "ck_avail_group_day_dom"
                        CHECK ("day_of_month" BETWEEN 1 AND 31),
                    CONSTRAINT "ck_avail_group_day_kind_interval"
                        CHECK (
                            ("kind" = 'INT' AND "interval_str" IS NOT NULL AND length(trim("interval_str")) >= 11)
                            OR ("kind" IN ('ANY', 'NONE') AND "interval_str" IS NULL)
                        )
                );

                INSERT INTO "__temp_availability_group_day" (
                    "id",
                    "availability_group_member_id",
                    "day_of_month",
                    "kind",
                    "interval_str"
                )
                SELECT
                    "id",
                    "availability_group_member_id",
                    "day_of_month",
                    "kind",
                    CASE WHEN "kind" = 'NONE' THEN NULL ELSE "interval_str" END
                FROM "availability_group_day";

                DROP TABLE "availability_group_day";
                ALTER TABLE "__temp_availability_group_day" RENAME TO "availability_group_day";

                CREATE UNIQUE INDEX "ux_avail_group_day_member_dom"
                    ON "availability_group_day" ("availability_group_member_id", "day_of_month");
                """);
        }
    }
}
