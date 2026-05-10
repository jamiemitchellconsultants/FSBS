using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgBalanceTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION fsbs.update_org_balance()
                RETURNS TRIGGER AS $$
                BEGIN
                    UPDATE fsbs.org_accounts oa
                    SET current_balance_gbp = (
                        SELECT COALESCE(SUM(i.net_gbp), 0)
                        FROM fsbs.invoices i
                        INNER JOIN fsbs.bookings b ON b.booking_id = i.booking_id
                        WHERE b.org_id = oa.org_id
                          AND i.status IN ('Issued', 'Overdue')
                          AND i.is_deleted = false
                    ) - (
                        SELECT COALESCE(SUM(p.amount_gbp), 0)
                        FROM fsbs.account_payments p
                        WHERE p.org_id = oa.org_id
                          AND p.status = 'Verified'
                          AND p.is_deleted = false
                    ),
                    updated_at = now()
                    WHERE oa.org_id = (
                        CASE
                            WHEN TG_TABLE_NAME = 'invoices' THEN
                                (SELECT b.org_id FROM fsbs.bookings b WHERE b.booking_id = COALESCE(NEW.booking_id, OLD.booking_id))
                            ELSE
                                COALESCE(NEW.org_id, OLD.org_id)
                        END
                    );
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_invoices_update_balance
                    AFTER INSERT OR UPDATE OR DELETE ON fsbs.invoices
                    FOR EACH ROW EXECUTE FUNCTION fsbs.update_org_balance();
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_payments_update_balance
                    AFTER INSERT OR UPDATE OR DELETE ON fsbs.account_payments
                    FOR EACH ROW EXECUTE FUNCTION fsbs.update_org_balance();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_payments_update_balance ON fsbs.account_payments;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_invoices_update_balance ON fsbs.invoices;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fsbs.update_org_balance();");
        }
    }
}
