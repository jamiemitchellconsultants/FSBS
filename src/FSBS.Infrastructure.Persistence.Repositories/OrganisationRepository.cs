using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class OrganisationRepository(FsbsDbContext db) : IOrganisationRepository
{
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
