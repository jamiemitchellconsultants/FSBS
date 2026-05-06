namespace FSBS.Domain.Events;

/// <summary>
/// Marker interface for all domain events. Implement on record types that
/// represent something meaningful that happened within the domain.
/// Events are collected on aggregate roots and dispatched after the DB
/// transaction commits via <c>IDomainEventDispatcher</c>.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp at which the event occurred, set at construction time.</summary>
    DateTimeOffset OccurredAt { get; }
}
