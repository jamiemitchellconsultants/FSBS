using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Read-side repository for reconfiguration duration templates.
/// Used by <c>ReconfigurationService</c> to determine turnaround time between
/// two simulator configurations.
/// </summary>
public interface IReconfigurationTemplateRepository
{
    /// <summary>
    /// Returns the reconfiguration template for the given configuration pair,
    /// or null if no explicit template exists (caller falls back to
    /// <see cref="SimulatorUnit.DefaultReconfigMins"/>).
    /// </summary>
    Task<ReconfigurationTemplate?> FindAsync(
        Guid fromConfigurationId,
        Guid toConfigurationId,
        CancellationToken ct = default);
}
