namespace FSBS.Web.Services;

public sealed class AvailabilityService(HttpClient http)
{
    public Task<object?> GetAvailabilityAsync(Guid simulatorId, DateOnly weekStart, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);
}
