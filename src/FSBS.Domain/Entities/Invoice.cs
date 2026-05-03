using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A billing document raised against an <see cref="Organisation"/> for a
/// completed or invoiced <see cref="Booking"/>. Issued by SalesStaff once
/// a session reaches the <c>Invoiced</c> booking status.
/// </summary>
/// <remarks>
/// <para>
/// <b>Balance calculation:</b> <see cref="NetGbp"/> contributes to
/// <see cref="OrgAccount.CurrentBalanceGbp"/> when the invoice status is
/// <c>Issued</c> or <c>Overdue</c>. The PostgreSQL balance trigger
/// (<c>update_org_balance</c>) fires on every insert/update/delete to keep
/// the running total current.
/// </para>
/// <para>
/// <b>Net constraint:</b> <c>net_gbp = gross_gbp − discount_gbp</c> is
/// enforced by the <c>ck_invoices_net</c> CHECK constraint at the database level.
/// </para>
/// <para>
/// Row-level security in PostgreSQL restricts invoice visibility to the
/// owning organisation's tenant.
/// </para>
/// </remarks>
public class Invoice : AuditableEntity, ISoftDeletable
{
    /// <summary>The booking this invoice was raised for.</summary>
    public Guid BookingId { get; set; }

    /// <summary>The organisation billed by this invoice.</summary>
    public Guid OrgId { get; set; }

    /// <summary>
    /// Human-readable invoice reference displayed on the document and in
    /// the customer portal (e.g. "INV-2024-00142").
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status in the invoice lifecycle:
    /// <c>Draft → Issued → Paid | Overdue | Voided</c>.
    /// Only <c>Issued</c> and <c>Overdue</c> invoices contribute to the
    /// organisation's outstanding balance.
    /// </summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>Total pre-discount amount in GBP.</summary>
    public decimal GrossGbp { get; set; }

    /// <summary>Total discount applied in GBP.</summary>
    public decimal DiscountGbp { get; set; }

    /// <summary>
    /// Final invoiceable amount in GBP (<c>GrossGbp − DiscountGbp</c>).
    /// Enforced by <c>ck_invoices_net</c> CHECK constraint.
    /// </summary>
    public decimal NetGbp { get; set; }

    /// <summary>The calendar date on which the invoice was issued to the organisation.</summary>
    public DateOnly IssuedOn { get; set; }

    /// <summary>Payment due date. Invoices past this date are marked <c>Overdue</c>.</summary>
    public DateOnly DueOn { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the booked session.</summary>
    public Booking Booking { get; set; } = null!;

    /// <summary>Navigation to the billed organisation.</summary>
    public Organisation Organisation { get; set; } = null!;

    /// <summary>Payment allocations that partially or fully settle this invoice.</summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
}
