namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

public interface IOrganisationRepository
{
    Task<IReadOnlyList<OrganisationSummary>> ListSummariesAsync(CancellationToken ct = default);
    Task<OrganisationSummary?> FindByIdAsync(Guid orgId, CancellationToken ct = default);
}
