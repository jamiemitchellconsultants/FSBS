using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// An immutable audit snapshot of a single <see cref="DiscountRule"/> applied
/// to a <see cref="Booking"/> at the point of confirmation. One row is written
/// for each rule that contributed to the final price; the records are never
/// updated or deleted.
/// </summary>
/// <remarks>
/// <para>
/// Because this entity is write-once it does not inherit from
/// <see cref="AuditableEntity"/> and carries no <c>UpdatedAt</c>,
/// <c>UpdatedBy</c>, or <c>IsDeleted</c> columns. EF Core is configured with
/// <c>PropertySaveBehavior.Ignore</c> on <see cref="CreatedAt"/> to prevent
/// accidental overwrites after the initial insert.
/// </para>
/// <para>
/// The <see cref="DiscountPct"/> and <see cref="DiscountAmountGbp"/> values
/// are snapshots taken at confirmation time. Even if the source
/// <see cref="DiscountRule"/> is later modified or deleted, these figures remain
/// correct for audit and invoice purposes.
/// </para>
/// </remarks>
public class BookingDiscount : EntityBase
{
    /// <summary>The booking this discount was applied to.</summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// The discount rule that was applied. Stored as a FK for traceability;
    /// the actual percentage and amount are snapshotted on this record.
    /// </summary>
    public Guid DiscountRuleId { get; set; }

    /// <summary>
    /// Category of the discount at the time of application. Snapshotted so
    /// that reports remain accurate if the rule's type is later changed.
    /// </summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// Percentage reduction that was applied (0–100), snapshotted from the
    /// rule at confirmation time.
    /// </summary>
    public decimal DiscountPct { get; set; }

    /// <summary>
    /// Actual monetary value of the discount in GBP. Derived from
    /// <see cref="DiscountPct"/> applied to the booking's gross price at
    /// confirmation time.
    /// </summary>
    public decimal DiscountAmountGbp { get; set; }

    /// <summary>UTC timestamp at which the booking was confirmed and this record was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Navigation to the parent booking.</summary>
    public Booking Booking { get; set; } = null!;

    /// <summary>Navigation to the source discount rule.</summary>
    public DiscountRule DiscountRule { get; set; } = null!;
}
