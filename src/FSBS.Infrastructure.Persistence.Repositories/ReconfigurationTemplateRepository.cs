using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class ReconfigurationTemplateRepository(FsbsDbContext db) : IReconfigurationTemplateRepository
{
    public Task<ReconfigurationTemplate?> FindAsync(
        Guid fromConfigurationId,
        Guid toConfigurationId,
        CancellationToken ct = default) =>
        db.ReconfigurationTemplates
            .FirstOrDefaultAsync(
                t => t.FromConfigId == fromConfigurationId
                  && t.ToConfigId   == toConfigurationId,
                ct);
}
