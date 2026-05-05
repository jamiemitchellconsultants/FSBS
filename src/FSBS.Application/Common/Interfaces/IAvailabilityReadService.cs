using FSBS.Application.Simulators.Queries;

namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Dapper-based read service that executes the complex availability query
/// bypassing EF Core. Returns the full slot grid in a single round-trip.
/// </summary>
public interface IAvailabilityReadService
{
    /// <summary>
    /// Returns the availability grid for the given simulator and date range,
    /// including available slots, reconfiguration windows, and maintenance windows.
    /// </summary>
    Task<AvailabilityGridDto> GetAvailabilityAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
