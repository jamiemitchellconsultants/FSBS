using System.Globalization;
using System.Net.Http.Json;

namespace FSBS.Web.Services;

public sealed class PricingService(HttpClient http)
{
    public async Task<decimal> GetQuoteForConfigAsync(
        Guid configurationId,
        string trainingType,
        int studentCount,
        DateTimeOffset slotStart,
        int durationMins,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"v1/pricing/quote" +
                      $"?configurationId={configurationId}" +
                      $"&trainingType={Uri.EscapeDataString(trainingType)}" +
                      $"&studentCount={studentCount}" +
                      $"&durationMins={durationMins}" +
                      $"&slotStart={Uri.EscapeDataString(slotStart.ToString("O", CultureInfo.InvariantCulture))}";

            var quote = await http.GetFromJsonAsync<PricingQuoteResponse>(url, ct);
            if (quote?.NetPriceGbp is { } net)
                return net;
        }
        catch
        {
            // Fallback keeps wizard usable in local dev without a live pricing policy.
        }

        var hours = Math.Max(durationMins / 60m, 4m);
        return Math.Round(hours * 120m * Math.Max(studentCount, 1), 2);
    }

    private sealed record PricingQuoteResponse(decimal? NetPriceGbp);
}
