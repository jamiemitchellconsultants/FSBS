namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a booking is cancelled, either by the customer or by an admin.
/// Triggers slot release, orphaned reconfiguration slot cleanup, and cancellation
/// notification.
/// </summary>
public record BookingCancelledEvent(
    Guid BookingId,
    Guid BookedBy,
    Guid CancelledBy,
    bool CancelledByAdmin,
    string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
