using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class AircraftTypeRepository(FsbsDbContext db) : IAircraftTypeRepository
{
    public async Task<IReadOnlyList<AircraftType>> ListAllAsync(CancellationToken ct = default) =>
        await db.AircraftTypes
            .OrderBy(a => a.IcaoCode)
            .ToListAsync(ct);

    public Task<AircraftType?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AircraftTypes.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(AircraftType aircraftType, CancellationToken ct = default) =>
        await db.AircraftTypes.AddAsync(aircraftType, ct);
}
