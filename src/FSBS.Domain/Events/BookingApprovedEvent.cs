namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a SalesStaff or SystemAdmin approves a PendingApproval booking.
/// Triggers the approval email to the InternalStudent.
/// </summary>
public record BookingApprovedEvent(
    Guid BookingId,
    Guid BookedBy,
    Guid ReviewedBy) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
