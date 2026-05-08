using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;

namespace FSBS.Infrastructure.Availability;

/// <summary>
/// No-op fallback used when no Redis connection string is configured. Returning
/// null from <see cref="GetAsync"/> matches a cache miss, so callers fall back
/// to the underlying read path. Production deployments must configure Redis;
/// this implementation exists so dev/test environments without Redis still
/// satisfy the <see cref="IAvailabilityCache"/> dependency at startup.
/// </summary>
internal sealed class NoOpAvailabilityCache : IAvailabilityCache
{
    public Task<AvailabilityGridDto?> GetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default) =>
        Task.FromResult<AvailabilityGridDto?>(null);

    public Task SetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        AvailabilityGridDto grid,
        CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task InvalidateAsync(Guid simulatorId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
