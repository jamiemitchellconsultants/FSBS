using FSBS.Application.Simulators.Queries;

namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Redis-backed cache for the simulator availability grid.
/// TTL is 60 seconds. Every booking mutation must call
/// <see cref="InvalidateAsync"/> so the next read rebuilds from the DB.
/// </summary>
public interface IAvailabilityCache
{
    /// <summary>
    /// Returns the cached availability grid for the given simulator and date
    /// range, or null if the cache entry has expired or does not exist.
    /// </summary>
    Task<AvailabilityGridDto?> GetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>
    /// Stores the availability grid in Redis with a 60-second TTL.
    /// </summary>
    Task SetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        AvailabilityGridDto grid,
        CancellationToken ct = default);

    /// <summary>
    /// Removes all cache entries for the given simulator. Called on every
    /// booking create, modify, or cancel so the next read is fresh.
    /// </summary>
    Task InvalidateAsync(Guid simulatorId, CancellationToken ct = default);
}
