namespace FSBS.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
