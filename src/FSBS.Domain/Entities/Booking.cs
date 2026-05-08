using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// The central aggregate of the FSBS domain. Represents a customer's or staff
/// member's reservation of simulator time, together with all associated slots,
/// pricing, approvals, and notes.
/// </summary>
/// <remarks>
/// <para>
/// <b>State machine — external customers:</b>
/// <c>Provisional (15-min hold) → Confirmed → InProgress → Completed → Invoiced</c>.
/// A provisional booking expires automatically if not confirmed within the hold window.
/// </para>
/// <para>
/// <b>State machine — InternalStudent:</b>
/// <c>PendingApproval (slot reserved, no expiry) → Confirmed | Rejected</c>.
/// InternalStudent bookings never enter <c>Provisional</c> and are exempt from
/// all discount rules; the staff rate applies exclusively.
/// </para>
/// <para>
/// <b>Price immutability:</b> <see cref="GrossPriceGbp"/>, <see cref="DiscountPct"/>,
/// and <see cref="NetPriceGbp"/> are set once when the booking transitions to
/// <c>Confirmed</c>. They must not be recalculated or overwritten thereafter.
/// </para>
/// <para>
/// <b>Idempotency:</b> the <see cref="IdempotencyKey"/> UUID is required on every
/// <c>POST /bookings</c> request via the <c>Idempotency-Key</c> header. A unique
/// index on this column allows safe client retries without creating duplicate bookings.
/// </para>
/// </remarks>
public class Booking : AggregateRoot, ISoftDeletable
{
    /// <summary>
    /// <see cref="AppUser.Id"/> of the user who created the booking.
    /// For corporate bookings this may be a CorporateManager booking on
    /// behalf of their organisation's students.
    /// </summary>
    public Guid BookedBy { get; set; }

    /// <summary>
    /// Organisation on whose account this booking will be invoiced.
    /// <c>null</c> for private customer or InternalStudent bookings.
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// <see cref="AppRole"/> of the user at the time of booking. Determines
    /// which state machine path is followed and which pricing rules apply.
    /// </summary>
    public AppRole BookerRole { get; set; }

    /// <summary>
    /// Whether this is a Flight Deck or Cabin Crew session. Constrains the
    /// eligible simulator configurations, instructor assignments, and student
    /// capacity limits.
    /// </summary>
    public TrainingType TrainingType { get; set; }

    /// <summary>
    /// The simulator configuration selected for this booking. Determines the
    /// applicable <see cref="PricingPolicy"/> and maximum capacity.
    /// </summary>
    public Guid ConfigId { get; set; }

    /// <summary>
    /// The enrolment this booking is associated with, when the session is part
    /// of a structured course. <c>null</c> for ad-hoc bookings.
    /// </summary>
    public Guid? EnrolmentId { get; set; }

    /// <summary>
    /// Number of students attending the session. Must be ≥ 1, ≤ 4 for
    /// Flight Deck bookings, and ≤ 10 for Cabin Crew bookings. Enforced by
    /// domain validators and PostgreSQL CHECK constraints.
    /// </summary>
    public int StudentCount { get; set; }

    /// <summary>Current lifecycle state of the booking.</summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Pre-discount total price in GBP. Set at confirmation; <c>null</c> before
    /// the booking is confirmed. Never recalculated after confirmation.
    /// </summary>
    public decimal? GrossPriceGbp { get; set; }

    /// <summary>
    /// Discount percentage applied (0–100). Set at confirmation; <c>null</c> before
    /// confirmation or when no discount applies.
    /// </summary>
    public decimal? DiscountPct { get; set; }

    /// <summary>
    /// Final invoiceable amount in GBP (<c>GrossPriceGbp − DiscountGbp</c>).
    /// Set at confirmation; <c>null</c> before confirmation.
    /// </summary>
    public decimal? NetPriceGbp { get; set; }

    /// <summary>
    /// The internal cost centre charged for this booking.
    /// Mandatory for <c>InternalStudent</c> bookings; <c>null</c> otherwise.
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Internal budget code for financial reporting. Mandatory for
    /// <c>InternalStudent</c> bookings; <c>null</c> otherwise.
    /// </summary>
    public string? BudgetCode { get; set; }

    /// <summary>
    /// Client-supplied UUID that guarantees exactly-once semantics for booking
    /// creation. Sourced from the <c>Idempotency-Key</c> HTTP header. A unique
    /// database index prevents duplicate rows even under concurrent retries.
    /// </summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>
    /// UTC timestamp at which a <c>Provisional</c> booking expires and the
    /// reserved slot is released. <c>null</c> for non-provisional bookings.
    /// </summary>
    public DateTimeOffset? ProvisionalExpiresAt { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the simulator configuration selected for this booking.</summary>
    public SimulatorConfiguration Configuration { get; set; } = null!;

    /// <summary>Navigation to the enrolment this booking is linked to, if any.</summary>
    public Enrolment? Enrolment { get; set; }

    /// <summary>The one or more time slots reserved in a simulator bay for this booking.</summary>
    public ICollection<BookingSlot> Slots { get; set; } = [];

    /// <summary>Internal notes attached to this booking by staff.</summary>
    public ICollection<BookingNote> Notes { get; set; } = [];

    /// <summary>
    /// Immutable discount snapshots written at confirmation. One entry per
    /// <see cref="DiscountRule"/> that was applied.
    /// </summary>
    public ICollection<BookingDiscount> Discounts { get; set; } = [];

    /// <summary>
    /// Approval record created when the booking enters <c>PendingApproval</c>.
    /// Only present for <c>InternalStudent</c> bookings.
    /// </summary>
    public BookingApproval? Approval { get; set; }
}
