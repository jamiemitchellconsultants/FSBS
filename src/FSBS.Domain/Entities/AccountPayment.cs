using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A payment posted against an <see cref="OrgAccount"/> by SalesStaff on
/// behalf of the organisation. Payments start in <c>Pending</c> status and
/// only affect <see cref="OrgAccount.CurrentBalanceGbp"/> once a
/// Management or SystemAdmin user moves them to <c>Verified</c>.
/// </summary>
/// <remarks>
/// <b>Status lifecycle:</b> <c>Pending → Verified | Voided</c>.
/// Voiding requires a mandatory reason and is restricted to Management
/// and SystemAdmin roles. A <c>PaymentVerified</c> or <c>PaymentVoided</c>
/// notification is dispatched to the CorporateManager on each transition.
/// The PostgreSQL balance trigger fires on <c>Verified</c> and <c>Voided</c>
/// transitions to keep <see cref="OrgAccount.CurrentBalanceGbp"/> consistent.
/// </remarks>
public class AccountPayment : AuditableEntity, ISoftDeletable
{
    /// <summary>The account this payment is applied to.</summary>
    public Guid OrgAccountId { get; set; }

    /// <summary>The amount received in GBP. Always positive.</summary>
    public decimal AmountGbp { get; set; }

    /// <summary>
    /// How the payment was made. Drives the information required at entry
    /// (e.g. bank reference for <c>BankTransfer</c>, cheque number for <c>Cheque</c>).
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Approval state of the payment. Only <c>Verified</c> payments reduce the
    /// organisation's outstanding balance.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Payment reference supplied by the organisation (e.g. bank transfer
    /// reference, cheque number). Used for reconciliation.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>Free-text notes recorded by SalesStaff at time of entry.</summary>
    public string? Notes { get; set; }

    /// <summary><see cref="AppUser.Id"/> of the user who verified the payment.</summary>
    public Guid? VerifiedBy { get; set; }

    /// <summary>UTC timestamp at which the payment was verified.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>
    /// Mandatory explanation when voiding a payment. Must be provided by
    /// the Management or SystemAdmin user performing the void action.
    /// </summary>
    public string? VoidReason { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the account this payment belongs to.</summary>
    public OrgAccount OrgAccount { get; set; } = null!;

    /// <summary>Invoice allocations that distribute this payment across invoices.</summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
}
