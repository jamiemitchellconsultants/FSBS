using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

public interface ISimulatorRepository
{
    /// <summary>Returns the unit with its Bays and ActiveConfiguration loaded.</summary>
    Task<SimulatorUnit?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<SimulatorBay?> FindBayAsync(Guid bayId, CancellationToken ct = default);

    Task<SimulatorConfiguration?> FindConfigurationAsync(Guid configurationId, CancellationToken ct = default);

    /// <summary>
    /// Returns all simulator configurations that support the given training type,
    /// ordered by aircraft type. Used to populate the booking wizard step.
    /// </summary>
    Task<IReadOnlyList<SimulatorConfiguration>> ListConfigurationsForTrainingTypeAsync(
        Enums.TrainingType trainingType,
        CancellationToken ct = default);
}
