using FSBS.Application.Common.Events;
using FSBS.Domain.Events;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.Common.Services;

/// <summary>
/// Dispatches domain events via MediatR by wrapping each event in a
/// DomainEventNotification so that handlers can subscribe per concrete type.
/// </summary>
public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    /// <inheritdoc/>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(@event.GetType());
            var notification = Activator.CreateInstance(notificationType, @event)!;
            await publisher.Publish(notification, ct);
        }
    }
}
