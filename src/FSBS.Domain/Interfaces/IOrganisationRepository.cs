using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the Organisation aggregate. Used by command handlers.
/// </summary>
public interface IOrganisationRepository
{
    /// <summary>Returns the organisation by its primary key, or null if not found.</summary>
    Task<Organisation?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the organisation together with its account. Used when
    /// evaluating corporate discount eligibility and credit limits.
    /// </summary>
    Task<Organisation?> FindWithAccountAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds the organisation to the change tracker for insertion on next SaveChanges.</summary>
    Task AddAsync(Organisation organisation, CancellationToken ct = default);
}
