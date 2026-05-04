namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a SalesStaff or SystemAdmin rejects a PendingApproval booking.
/// Triggers the rejection email (including reason) and slot release.
/// </summary>
public record BookingRejectedEvent(
    Guid BookingId,
    Guid BookedBy,
    Guid ReviewedBy,
    string RejectionReason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
