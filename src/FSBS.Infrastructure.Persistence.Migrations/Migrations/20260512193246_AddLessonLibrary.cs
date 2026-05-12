using System;
using FSBS.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_template_id",
                schema: "fsbs",
                table: "lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lesson_templates",
                schema: "fsbs",
                columns: table => new
                {
                    lesson_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    training_type = table.Column<TrainingType>(type: "fsbs.training_type", nullable: false),
                    default_min_duration_mins = table.Column<int>(type: "integer", nullable: false),
                    requires_instructor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_mandatory_by_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lesson_templates", x => x.lesson_template_id);
                    table.CheckConstraint("ck_lesson_templates_duration", "default_min_duration_mins > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_lessons_source_template_id",
                schema: "fsbs",
                table: "lessons",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_lesson_templates_filter",
                schema: "fsbs",
                table: "lesson_templates",
                columns: new[] { "tenant_id", "training_type", "is_active", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "uq_lesson_templates_tenant_title_active",
                schema: "fsbs",
                table: "lesson_templates",
                columns: new[] { "tenant_id", "title" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_lessons_lesson_templates_source_template_id",
                schema: "fsbs",
                table: "lessons",
                column: "source_template_id",
                principalSchema: "fsbs",
                principalTable: "lesson_templates",
                principalColumn: "lesson_template_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lessons_lesson_templates_source_template_id",
                schema: "fsbs",
                table: "lessons");

            migrationBuilder.DropTable(
                name: "lesson_templates",
                schema: "fsbs");

            migrationBuilder.DropIndex(
                name: "ix_lessons_source_template_id",
                schema: "fsbs",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "source_template_id",
                schema: "fsbs",
                table: "lessons");
        }
    }
}
