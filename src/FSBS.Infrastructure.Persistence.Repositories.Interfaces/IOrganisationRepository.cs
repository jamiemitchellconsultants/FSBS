namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

/// <summary>
/// Read-side repository for organisation lookups used by query handlers and
/// invitation flows. Operates across all tenants (no tenant filter applied).
/// </summary>
public interface IOrganisationRepository
{
    /// <summary>Returns all non-deleted organisations sorted by name.</summary>
    Task<IReadOnlyList<OrganisationSummary>> ListSummariesAsync(CancellationToken ct = default);

    /// <summary>Returns the name and ID of a single organisation, or <c>null</c> if not found.</summary>
    Task<OrganisationSummary?> FindByIdAsync(Guid orgId, CancellationToken ct = default);
}
