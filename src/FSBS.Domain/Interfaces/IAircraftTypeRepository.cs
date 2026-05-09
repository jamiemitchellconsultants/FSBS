using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

public interface IAircraftTypeRepository
{
    Task<IReadOnlyList<AircraftType>> ListAllAsync(CancellationToken ct = default);
    Task<AircraftType?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AircraftType aircraftType, CancellationToken ct = default);
}
