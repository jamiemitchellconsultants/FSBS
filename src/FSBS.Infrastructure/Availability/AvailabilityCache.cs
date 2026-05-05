using System.Text.Json;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;
using StackExchange.Redis;

namespace FSBS.Infrastructure.Availability;

/// <summary>
/// Redis-backed implementation of <see cref="IAvailabilityCache"/>.
/// Cache keys follow the pattern:
///   <c>availability:{simulatorId}:{fromTicks}:{toTicks}</c>
/// A wildcard scan on <c>availability:{simulatorId}:*</c> is used for
/// invalidation so all date-range variants are evicted together.
/// TTL is 60 seconds as required by the spec.
/// </summary>
internal sealed class AvailabilityCache(IConnectionMultiplexer redis) : IAvailabilityCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<AvailabilityGridDto?> GetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var key = BuildKey(simulatorId, from, to);
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<AvailabilityGridDto>((string)value!);
    }

    public async Task SetAsync(
        Guid simulatorId,
        DateTimeOffset from,
        DateTimeOffset to,
        AvailabilityGridDto grid,
        CancellationToken ct = default)
    {
        var key = BuildKey(simulatorId, from, to);
        var value = JsonSerializer.Serialize(grid);
        await _db.StringSetAsync(key, value, Ttl);
    }

    public async Task InvalidateAsync(Guid simulatorId, CancellationToken ct = default)
    {
        // Scan all keys for this simulator across all date ranges and delete them.
        var server = redis.GetServer(redis.GetEndPoints()[0]);
        var pattern = $"availability:{simulatorId}:*";
        var keys = server.Keys(pattern: pattern).ToArray();
        if (keys.Length > 0)
            await _db.KeyDeleteAsync(keys);
    }

    private static string BuildKey(Guid simulatorId, DateTimeOffset from, DateTimeOffset to) =>
        $"availability:{simulatorId}:{from.UtcTicks}:{to.UtcTicks}";
}
