using FSBS.Domain.Events;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Dispatches domain events collected on aggregate roots after the database
/// transaction commits. Implemented in the Application layer using MediatR.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
