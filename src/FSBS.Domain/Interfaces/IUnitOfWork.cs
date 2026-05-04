using FSBS.Domain.Events;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Abstracts the database commit operation so that the TransactionBehaviour
/// can flush all pending changes without depending on EF Core directly.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Collects all pending domain events from tracked aggregate roots and
    /// clears them so they cannot be dispatched twice. Call before
    /// <see cref="SaveChangesAsync"/> so events are captured even if the save throws.
    /// </summary>
    IReadOnlyList<IDomainEvent> CollectAndClearDomainEvents();
}
