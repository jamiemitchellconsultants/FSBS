namespace FSBS.Domain.Entities;

/// <summary>
/// Distributes a portion of an <see cref="AccountPayment"/> against a specific
/// <see cref="Invoice"/>, enabling a single payment to partially or fully settle
/// multiple invoices.
/// </summary>
/// <remarks>
/// The FK from <c>payment_allocations.invoice_id</c> to <c>invoices</c> is added
/// via a deferred <c>ALTER TABLE</c> in the DDL because <c>invoices</c> is defined
/// later in the schema script. This ordering is preserved in the EF migration.
/// </remarks>
public class PaymentAllocation : AuditableEntity
{
    /// <summary>The payment being allocated.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>The invoice being (partially) settled.</summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// The portion of the payment applied to this invoice in GBP. The sum
    /// of all allocations for a payment cannot exceed the payment's
    /// <see cref="AccountPayment.AmountGbp"/>.
    /// </summary>
    public decimal AmountGbp { get; set; }

    /// <summary>Navigation to the source payment.</summary>
    public AccountPayment Payment { get; set; } = null!;

    /// <summary>Navigation to the invoice being settled.</summary>
    public Invoice Invoice { get; set; } = null!;
}
