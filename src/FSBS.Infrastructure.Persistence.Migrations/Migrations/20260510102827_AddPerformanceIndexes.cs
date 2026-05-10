using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Filtered index for the pending-approval queue — used by SalesStaff approval list.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_bookings_status
                    ON fsbs.bookings (status)
                    WHERE is_deleted = false;
                """);

            // Descending index on created_at scoped to PendingApproval rows — supports
            // the approval queue ordered by submission time.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_bookings_pending_approval
                    ON fsbs.bookings (created_at DESC)
                    WHERE status = 'PendingApproval' AND is_deleted = false;
                """);

            // Filtered index on payment status — used when the balance trigger and
            // payment verification queries filter by status = 'Verified'.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_account_payments_status
                    ON fsbs.account_payments (status)
                    WHERE is_deleted = false;
                """);

            // Filtered index on tenant_id — used by the EF global query filter on
            // organisations which always includes a tenant_id predicate.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_organisations_tenant_id
                    ON fsbs.organisations (tenant_id)
                    WHERE is_deleted = false;
                """);

            // Unique constraint ensuring one invoice per booking.
            migrationBuilder.Sql("""
                ALTER TABLE fsbs.invoices
                    ADD CONSTRAINT uq_invoices_booking UNIQUE (booking_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE fsbs.invoices DROP CONSTRAINT IF EXISTS uq_invoices_booking;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS fsbs.ix_organisations_tenant_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS fsbs.ix_account_payments_status;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS fsbs.ix_bookings_pending_approval;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS fsbs.ix_bookings_status;");
        }
    }
}
