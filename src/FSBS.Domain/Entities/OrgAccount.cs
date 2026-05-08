using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Financial account held for an <see cref="Organisation"/>. Tracks the credit
/// limit, live balance, and account status used to gate new bookings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Balance maintenance:</b> <see cref="CurrentBalanceGbp"/> is maintained
/// exclusively by a PostgreSQL trigger (<c>update_org_balance()</c>) that fires
/// after any insert, update, or delete on <c>invoices</c> or
/// <c>account_payments</c>. Application code must never write to this column
/// directly. EF Core is configured with <c>ValueGeneratedOnAddOrUpdate</c> and
/// <c>PropertySaveBehavior.Ignore</c> to prevent accidental overwrites.
/// </para>
/// <para>
/// <b>Balance formula:</b>
/// <c>SUM(net_gbp WHERE invoice status IN ('Issued','Overdue'))
///  − SUM(amount_gbp WHERE payment status = 'Verified')</c>
/// </para>
/// <para>
/// A nightly Lambda reconciliation job cross-checks the trigger value against
/// a full SUM query and raises a CloudWatch alarm on any discrepancy.
/// </para>
/// </remarks>
public class OrgAccount : AuditableEntity
{
    /// <summary>The organisation that owns this account.</summary>
    public Guid OrgId { get; set; }

    /// <summary>
    /// Maximum outstanding balance (GBP) the school will extend to this
    /// organisation before blocking new bookings. Configured by SalesStaff.
    /// </summary>
    public decimal CreditLimitGbp { get; set; }

    /// <summary>
    /// Current outstanding balance in GBP. Positive means the organisation
    /// owes money; negative means they are in credit.
    /// <b>Read-only from the application layer</b> — maintained by a
    /// PostgreSQL trigger. A <c>AccountBalanceWarning</c> event is dispatched
    /// when this exceeds 80 % of <see cref="CreditLimitGbp"/>.
    /// </summary>
    public decimal CurrentBalanceGbp { get; set; }

    /// <summary>
    /// Lifecycle status of the account. A <c>Suspended</c> or <c>Closed</c>
    /// account blocks new bookings for the organisation.
    /// </summary>
    public AccountStatus Status { get; set; }
    /// <summary>
    /// Number of days after invoice issue date that payment is due.
    /// Defaults to 30. Used when calculating invoice due dates.
    /// </summary>
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>Navigation to the owning organisation.</summary>
    public Organisation Organisation { get; set; } = null!;

    /// <summary>All payment records posted against this account.</summary>
    public ICollection<AccountPayment> Payments { get; set; } = [];

    /// <summary>Point-in-time statement snapshots generated for this account.</summary>
    public ICollection<AccountStatement> Statements { get; set; } = [];
}
