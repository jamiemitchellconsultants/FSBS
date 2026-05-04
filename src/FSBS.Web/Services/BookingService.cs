using System.Net.Http.Json;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;

namespace FSBS.Web.Services;

public sealed class BookingService(HttpClient http)
{
    public async Task<IReadOnlyList<BookingSummaryDto>> GetBookingsForRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        var dateFrom = from.UtcDateTime.ToString("yyyy-MM-dd");
        var dateTo = to.UtcDateTime.ToString("yyyy-MM-dd");

        try
        {
            var result = await http.GetFromJsonAsync<PagedResult<BookingSummaryDto>>(
                $"v1/bookings?from={Uri.EscapeDataString(dateFrom)}&to={Uri.EscapeDataString(dateTo)}&limit=500",
                ct);

            if (result?.Items is { Count: > 0 })
            {
                return result.Items;
            }
        }
        catch
        {
            // Fallback for scaffolding environments that only expose /bookings/range.
        }

        return await GetMyBookingsForRangeAsync(from, to, ct);
    }

    public async Task<PagedResult<BookingSummaryDto>> GetMyBookingsAsync(
        string? afterCursor = null, int limit = 20, CancellationToken ct = default)
    {
        var url = $"v1/bookings?limit={limit}";
        if (afterCursor is not null)
            url += $"&after={Uri.EscapeDataString(afterCursor)}";

        var result = await http.GetFromJsonAsync<PagedResult<BookingSummaryDto>>(url, ct);
        return result ?? new PagedResult<BookingSummaryDto>([], null);
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> GetMyBookingsForRangeAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var url = $"v1/bookings/range?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        var result = await http.GetFromJsonAsync<IReadOnlyList<BookingSummaryDto>>(url, ct);
        return result ?? [];
    }

    public async Task<BookingDetailDto?> GetBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<BookingDetailDto>($"v1/bookings/{bookingId}", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<Guid> CreateBookingAsync(object command, CancellationToken ct = default) =>
        Task.FromResult(Guid.Empty);

    public Task ApproveBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task CancelBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<object>> GetPendingApprovalsAsync(
        string? afterCursor = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);
}
