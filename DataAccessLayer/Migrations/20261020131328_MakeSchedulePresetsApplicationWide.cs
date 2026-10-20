using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class MakeSchedulePresetsApplicationWide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedule_preset_container_container_id",
                table: "schedule_preset");

            migrationBuilder.AlterColumn<int>(
                name: "container_id",
                table: "schedule_preset",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_preset_container_container_id",
                table: "schedule_preset",
                column: "container_id",
                principalTable: "container",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted origins cannot be reconstructed: restore a database backup before downgrading.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE schedule_preset_downgrade_guard (
                    valid INTEGER CONSTRAINT restore_database_backup_before_downgrading CHECK (valid = 1)
                );
                INSERT INTO schedule_preset_downgrade_guard (valid)
                    SELECT 0 WHERE EXISTS (SELECT 1 FROM schedule_preset WHERE container_id IS NULL);
                DROP TABLE schedule_preset_downgrade_guard;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_preset_container_container_id",
                table: "schedule_preset");

            migrationBuilder.AlterColumn<int>(
                name: "container_id",
                table: "schedule_preset",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_preset_container_container_id",
                table: "schedule_preset",
                column: "container_id",
                principalTable: "container",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
