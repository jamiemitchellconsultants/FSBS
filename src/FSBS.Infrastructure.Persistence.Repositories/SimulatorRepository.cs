using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class SimulatorRepository(FsbsDbContext db) : ISimulatorRepository
{
    public Task<SimulatorUnit?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SimulatorUnits
            .Include(u => u.Bays)
            .Include(u => u.Configurations)
                .ThenInclude(c => c.AircraftType)
            .Include(u => u.ActiveConfiguration)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<SimulatorBay?> FindBayAsync(Guid bayId, CancellationToken ct = default) =>
        db.SimulatorBays
            .Include(b => b.SimulatorUnit)
                .ThenInclude(u => u.ActiveConfiguration)
            .FirstOrDefaultAsync(b => b.Id == bayId, ct);

    public Task<SimulatorConfiguration?> FindConfigurationAsync(Guid configurationId, CancellationToken ct = default) =>
        db.SimulatorConfigurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, ct);

    public async Task<IReadOnlyList<SimulatorConfiguration>> ListConfigurationsForTrainingTypeAsync(
        TrainingType trainingType,
        CancellationToken ct = default) =>
        await db.SimulatorConfigurations
            .Where(c => c.SupportedTrainingTypes.Contains(trainingType))
            .OrderBy(c => c.AircraftType)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SimulatorUnit>> ListAllAsync(CancellationToken ct = default) =>
        await db.SimulatorUnits
            .Include(u => u.Bays)
            .Include(u => u.Configurations)
                .ThenInclude(c => c.AircraftType)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

    public Task AddUnitAsync(SimulatorUnit unit, CancellationToken ct = default) =>
        db.SimulatorUnits.AddAsync(unit, ct).AsTask();

    public Task AddBayAsync(SimulatorBay bay, CancellationToken ct = default) =>
        db.SimulatorBays.AddAsync(bay, ct).AsTask();

    public Task AddConfigurationAsync(SimulatorConfiguration configuration, CancellationToken ct = default) =>
        db.SimulatorConfigurations.AddAsync(configuration, ct).AsTask();
}
