using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorWeeklyPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "instructor_weekly_patterns",
                schema: "fsbs",
                columns: table => new
                {
                    pattern_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructor_weekly_patterns", x => x.pattern_id);
                    table.ForeignKey(
                        name: "fk_instructor_weekly_patterns_instructors_instructor_id",
                        column: x => x.instructor_id,
                        principalSchema: "fsbs",
                        principalTable: "instructors",
                        principalColumn: "instructor_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instructor_weekly_pattern_slots",
                schema: "fsbs",
                columns: table => new
                {
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pattern_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<short>(type: "smallint", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructor_weekly_pattern_slots", x => x.slot_id);
                    table.CheckConstraint("ck_pattern_slot_day", "day_of_week BETWEEN 0 AND 6");
                    table.CheckConstraint("ck_pattern_slot_half_hour_aligned", "extract(minute from start_time) IN (0, 30) AND extract(second from start_time) = 0 AND extract(minute from end_time) IN (0, 30) AND extract(second from end_time) = 0");
                    table.CheckConstraint("ck_pattern_slot_range", "end_time > start_time");
                    table.ForeignKey(
                        name: "fk_instructor_weekly_pattern_slots_instructor_weekly_patterns_",
                        column: x => x.pattern_id,
                        principalSchema: "fsbs",
                        principalTable: "instructor_weekly_patterns",
                        principalColumn: "pattern_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instructor_weekly_pattern_slots_pattern",
                schema: "fsbs",
                table: "instructor_weekly_pattern_slots",
                column: "pattern_id");

            migrationBuilder.CreateIndex(
                name: "uq_instructor_open_pattern",
                schema: "fsbs",
                table: "instructor_weekly_patterns",
                column: "instructor_id",
                unique: true,
                filter: "effective_to IS NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instructor_weekly_pattern_slots",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "instructor_weekly_patterns",
                schema: "fsbs");
        }
    }
}
