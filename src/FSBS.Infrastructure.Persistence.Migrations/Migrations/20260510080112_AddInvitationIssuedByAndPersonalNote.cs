using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationIssuedByAndPersonalNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_invitations_claimed_by",
                schema: "fsbs",
                table: "invitations",
                column: "claimed_by");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_issued_by",
                schema: "fsbs",
                table: "invitations",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_revoked_by",
                schema: "fsbs",
                table: "invitations",
                column: "revoked_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invitations_claimed_by",
                schema: "fsbs",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "ix_invitations_issued_by",
                schema: "fsbs",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "ix_invitations_revoked_by",
                schema: "fsbs",
                table: "invitations");
        }
    }
}
