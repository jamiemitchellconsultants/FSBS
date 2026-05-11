using System.Globalization;
using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed record PricingQuoteDto(
    decimal GrossPriceGbp,
    decimal DiscountPct,
    decimal NetPriceGbp);

public sealed class PricingService(HttpClient http)
{
    public async Task<PricingQuoteDto?> GetQuoteAsync(
        Guid configurationId,
        string trainingType,
        string customerClass,
        int durationMins,
        int studentCount,
        DateTimeOffset slotStart,
        Guid? orgId = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"v1/pricing/quote" +
                      $"?configurationId={configurationId}" +
                      $"&trainingType={Uri.EscapeDataString(trainingType)}" +
                      $"&customerClass={Uri.EscapeDataString(customerClass)}" +
                      $"&studentCount={studentCount}" +
                      $"&durationMins={durationMins}" +
                      $"&slotStart={Uri.EscapeDataString(slotStart.ToString("O", CultureInfo.InvariantCulture))}" +
                      (orgId.HasValue ? $"&orgId={orgId}" : string.Empty);

            return await http.GetFromJsonAsync<PricingQuoteDto>(url, ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convenience overload used by wizard steps that only need the net price.
    /// Falls back to a local estimate when no pricing policy is configured.
    /// </summary>
    public async Task<decimal> GetQuoteForConfigAsync(
        Guid configurationId,
        string trainingType,
        int studentCount,
        DateTimeOffset slotStart,
        int durationMins,
        CancellationToken ct = default)
    {
        var quote = await GetQuoteAsync(
            configurationId, trainingType, "Standard",
            durationMins, studentCount, slotStart, ct: ct);

        if (quote is not null)
            return quote.NetPriceGbp;

        // Fallback keeps wizard usable in local dev without a live pricing policy.
        var hours = Math.Max(durationMins / 60m, 4m);
        return Math.Round(hours * 120m * Math.Max(studentCount, 1), 2);
    }
}
