using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

public interface IOrganisationRepository
{
    Task<Organisation?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the organisation together with its account. Used when
    /// evaluating corporate discount eligibility and credit limits.
    /// </summary>
    Task<Organisation?> FindWithAccountAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Organisation organisation, CancellationToken ct = default);
}
