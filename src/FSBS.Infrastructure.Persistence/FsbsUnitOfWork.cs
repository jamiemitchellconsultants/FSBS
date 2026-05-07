using FSBS.Domain.Entities;
using FSBS.Domain.Events;
using FSBS.Domain.Interfaces;

namespace FSBS.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed <see cref="IUnitOfWork"/>. Collects pending domain events
/// from every tracked <see cref="AggregateRoot"/> and delegates the database
/// commit to <see cref="FsbsDbContext.SaveChangesAsync(CancellationToken)"/>.
/// </summary>
/// <remarks>
/// Registered as a scoped service alongside the <see cref="FsbsDbContext"/> so
/// the unit of work shares the request-scoped change tracker used by every
/// repository in the same MediatR pipeline.
/// </remarks>
internal sealed class FsbsUnitOfWork(FsbsDbContext db) : IUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> CollectAndClearDomainEvents()
    {
        var aggregates = db.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0)
            return [];

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        return events;
    }
}
