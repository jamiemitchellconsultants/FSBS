using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Records the SalesStaff or SystemAdmin decision (approve or reject) on an
/// InternalStudent booking that entered <c>PendingApproval</c> state.
/// One approval record exists per booking; it is created when the booking is
/// submitted and updated when the reviewing user acts on it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Self-approval ban:</b> <see cref="ReviewedBy"/> cannot equal
/// <see cref="RequestedBy"/>. This is enforced in the command handler and
/// reinforced by the <c>ck_booking_approvals_no_self_approval</c> CHECK
/// constraint in the database.
/// </para>
/// <para>
/// <b>Rejection reason:</b> when <see cref="Decision"/> is
/// <see cref="ApprovalDecision.Rejected"/>, <see cref="RejectionReason"/>
/// must be provided and must be at least 10 characters. Enforced by the
/// <c>ck_booking_approvals_rejection</c> CHECK constraint.
/// </para>
/// </remarks>
public class BookingApproval : AuditableEntity
{
    /// <summary>The booking this approval record belongs to.</summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the InternalStudent who submitted the booking.
    /// Used to enforce the self-approval ban.
    /// </summary>
    public Guid RequestedBy { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the SalesStaff or SystemAdmin user who acted
    /// on the approval. <c>null</c> while the decision is still
    /// <see cref="ApprovalDecision.Pending"/>.
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>UTC timestamp at which the reviewing user made their decision.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// The outcome of the review. Starts as <c>Pending</c>; transitions to
    /// <c>Approved</c> (booking moves to <c>Confirmed</c>) or
    /// <c>Rejected</c> (slot released, InternalStudent notified with reason).
    /// </summary>
    public ApprovalDecision Decision { get; set; }

    /// <summary>
    /// Mandatory explanation when <see cref="Decision"/> is
    /// <see cref="ApprovalDecision.Rejected"/>. Must be at least 10 characters.
    /// Included in the rejection notification email to the InternalStudent.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>Navigation to the booking under review.</summary>
    public Booking Booking { get; set; } = null!;
}
