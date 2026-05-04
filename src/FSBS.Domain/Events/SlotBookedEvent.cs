using FSBS.Domain.Enums;

namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a booking is created and a slot is reserved (either Provisional
/// or PendingApproval). Triggers availability cache invalidation and real-time
/// calendar delta push via SignalR.
/// </summary>
public record SlotBookedEvent(
    Guid BookingId,
    Guid BookedBy,
    AppRole BookerRole,
    TrainingType TrainingType,
    Guid ConfigurationId,
    int StudentCount,
    Guid? OrgId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
