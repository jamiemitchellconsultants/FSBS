using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOrganisationRepository"/>.
/// Uses <c>IgnoreQueryFilters</c> so staff can list all organisations regardless
/// of the tenant filter applied to the current request.
/// </summary>
internal sealed class OrganisationRepository(FsbsDbContext db) : IOrganisationRepository
{
    /// <summary>Returns all non-deleted organisations sorted by name.</summary>
    public async Task<IReadOnlyList<OrganisationSummary>> ListSummariesAsync(CancellationToken ct = default)
    {
        var rows = await db.Organisations
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Name)
            .Select(o => new OrganisationSummary(o.Id, o.Name))
            .ToListAsync(ct);

        return rows;
    }

    /// <summary>Returns the name and ID of a single organisation, or <c>null</c> if not found.</summary>
    public async Task<OrganisationSummary?> FindByIdAsync(Guid orgId, CancellationToken ct = default)
    {
        var org = await db.Organisations
            .IgnoreQueryFilters()
            .Where(o => o.Id == orgId && !o.IsDeleted)
            .Select(o => new OrganisationSummary(o.Id, o.Name))
            .FirstOrDefaultAsync(ct);

        return org;
    }
}
