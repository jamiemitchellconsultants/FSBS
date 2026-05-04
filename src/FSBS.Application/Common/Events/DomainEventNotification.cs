using FSBS.Domain.Events;
using MediatR;

namespace FSBS.Application.Common.Events;

/// <summary>
/// Wraps a domain event in a MediatR INotification so that application-layer
/// handlers can subscribe to specific event types via
/// INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;.
/// </summary>
public record DomainEventNotification<TEvent>(TEvent Event) : INotification
    where TEvent : IDomainEvent;
