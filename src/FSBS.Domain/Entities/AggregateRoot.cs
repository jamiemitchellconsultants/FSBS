using FSBS.Domain.Events;

namespace FSBS.Domain.Entities;

/// <summary>
/// Base class for aggregate roots. Extends <see cref="AuditableEntity"/> with
/// domain event collection so that handlers can publish side-effects after
/// the transaction commits.
/// </summary>
public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
