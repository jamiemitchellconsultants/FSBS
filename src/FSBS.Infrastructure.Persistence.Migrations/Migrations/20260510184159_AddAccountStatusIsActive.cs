using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountStatusIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "fsbs",
                table: "account_statuses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                schema: "fsbs",
                table: "account_statuses",
                keyColumn: "code",
                keyValue: "Active",
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                schema: "fsbs",
                table: "account_statuses",
                keyColumn: "code",
                keyValue: "Closed",
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                schema: "fsbs",
                table: "account_statuses",
                keyColumn: "code",
                keyValue: "Suspended",
                column: "is_active",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "fsbs",
                table: "account_statuses");
        }
    }
}
