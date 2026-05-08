using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for simulator aggregates. Used by command handlers
/// for configuration lookups and bay resolution.
/// </summary>
public interface ISimulatorRepository
{
    /// <summary>Returns the unit with its Bays and ActiveConfiguration loaded.</summary>
    Task<SimulatorUnit?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the bay with its parent SimulatorUnit (and its ActiveConfiguration) loaded.</summary>
    Task<SimulatorBay?> FindBayAsync(Guid bayId, CancellationToken ct = default);

    /// <summary>Returns a simulator configuration by its primary key, or null if not found.</summary>
    Task<SimulatorConfiguration?> FindConfigurationAsync(Guid configurationId, CancellationToken ct = default);

    /// <summary>
    /// Returns all simulator configurations that support the given training type,
    /// ordered by aircraft type. Used to populate the booking wizard step.
    /// </summary>
    Task<IReadOnlyList<SimulatorConfiguration>> ListConfigurationsForTrainingTypeAsync(
        Enums.TrainingType trainingType,
        CancellationToken ct = default);

    /// <summary>Returns all non-deleted simulator units with their Bays and Configurations loaded.</summary>
    Task<IReadOnlyList<SimulatorUnit>> ListAllAsync(CancellationToken ct = default);
}
