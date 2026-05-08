using FSBS.Domain.Events;

namespace FSBS.Domain.Tests.Bookings;

/// <summary>
/// AggregateRoot is the only place in the domain that carries behaviour worth
/// testing without a real handler — collecting domain events and clearing them
/// after dispatch is the contract MediatRDomainEventDispatcher relies on.
/// </summary>
[Trait("Category", "Unit")]
public class AggregateRootDomainEventsTests
{
    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var booking = new Booking();
        booking.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_AppendsInOrder()
    {
        var booking = new Booking { Id = Guid.NewGuid(), BookedBy = Guid.NewGuid() };
        var first = new TestEvent(1);
        var second = new TestEvent(2);

        booking.AddDomainEvent(first);
        booking.AddDomainEvent(second);

        booking.DomainEvents.Should().Equal(first, second);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheList()
    {
        var booking = new Booking();
        booking.AddDomainEvent(new TestEvent(1));
        booking.AddDomainEvent(new TestEvent(2));

        booking.ClearDomainEvents();

        booking.DomainEvents.Should().BeEmpty();
    }

    private sealed record TestEvent(int Sequence) : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    }
}
