using System.Globalization;
using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class PricingService(HttpClient http)
{
    public async Task<decimal?> GetQuoteAsync(Guid simulatorId, string trainingType, int studentCount, DateOnly date, CancellationToken ct = default)
    {
        // Backward-compatible helper retained for existing callers.
        return await GetPrivateCustomerQuoteAsync(
            simulatorId,
            trainingType,
            studentCount,
            date,
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            ct);
    }

    public async Task<decimal> GetPrivateCustomerQuoteAsync(
        Guid simulatorId,
        string trainingType,
        int studentCount,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        CancellationToken ct = default)
    {
        return await GetQuoteForRoleAsync(simulatorId, trainingType, studentCount, date, start, end, null, null, ct);
    }

    public async Task<decimal> GetQuoteForRoleAsync(
        Guid simulatorId,
        string trainingType,
        int studentCount,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        string? appRole,
        Guid? orgId,
        CancellationToken ct = default)
    {
        var startAt = new DateTimeOffset(date.ToDateTime(start), TimeSpan.Zero);
        var customerClass = appRole == "InternalStudent" ? "Staff" : orgId.HasValue ? "Corporate" : "Standard";

        try
        {
            var quote = await http.GetFromJsonAsync<PricingQuoteResponse>(
                $"v1/pricing/quote?configId={simulatorId}&trainingType={Uri.EscapeDataString(trainingType)}&studentCount={studentCount}&slotCount=1&slotStartDates={Uri.EscapeDataString(startAt.ToString("O", CultureInfo.InvariantCulture))}&customerClass={Uri.EscapeDataString(customerClass)}{(orgId.HasValue ? $"&orgId={orgId.Value}" : string.Empty)}",
                ct);

            if (quote?.NetAmountGbp is { } net)
            {
                return net;
            }
        }
        catch
        {
            // In local scaffolding the pricing endpoint may be absent; fallback keeps wizard usable.
        }

        var durationHours = Math.Max((decimal)(end - start).TotalHours, 4m);
        var baselineHourlyRate = appRole == "InternalStudent" ? 90m : orgId.HasValue ? 110m : 120m;
        return Math.Round(durationHours * baselineHourlyRate * Math.Max(studentCount, 1), 2);
    }

    private sealed record PricingQuoteResponse(decimal? NetAmountGbp);
}
