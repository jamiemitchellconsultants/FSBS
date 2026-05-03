namespace FSBS.Web.Services;

public sealed class PricingService(HttpClient http)
{
    public Task<decimal?> GetQuoteAsync(Guid simulatorId, string trainingType, int studentCount, DateOnly date, CancellationToken ct = default) =>
        Task.FromResult<decimal?>(null);
}
