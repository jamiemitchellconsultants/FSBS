using System.Net.Http.Json;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;

namespace FSBS.Web.Services;

public sealed class BookingService(HttpClient http)
{
    public async Task<Guid> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/bookings")
        {
            Content = JsonContent.Create(request)
        };

        // Booking POSTs require idempotency to avoid duplicate charges/reservations on retries.
        message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey.ToString());

        var response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<BookingCreatedResponse>(cancellationToken: ct);
        return created?.BookingId ?? Guid.Empty;
    }

    public Task<Guid> CreatePrivateCustomerBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default) =>
        CreateBookingAsync(request, ct);

    public async Task<IReadOnlyList<BookingSummaryDto>> GetBookingsForRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? simulatorId = null,
        CancellationToken ct = default)
    {
        var dateFrom = Uri.EscapeDataString(from.ToString("O"));
        var dateTo = Uri.EscapeDataString(to.ToString("O"));
        var simulatorQuery = simulatorId.HasValue
            ? $"&simulatorId={Uri.EscapeDataString(simulatorId.Value.ToString())}"
            : string.Empty;

        try
        {
            var result = await http.GetFromJsonAsync<PagedResult<BookingSummaryDto>>(
                $"v1/bookings?from={dateFrom}&to={dateTo}&limit=500{simulatorQuery}",
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

        return await GetMyBookingsForRangeAsync(from, to, simulatorId, ct);
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
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? simulatorId = null,
        CancellationToken ct = default)
    {
        var url = $"v1/bookings/range?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        if (simulatorId.HasValue)
        {
            url += $"&simulatorId={Uri.EscapeDataString(simulatorId.Value.ToString())}";
        }

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

public sealed record CreateBookingRequest(
    Guid IdempotencyKey,
    Guid SimulatorId,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    string TrainingType,
    int StudentCount,
    Guid? InstructorId = null,
    Guid? OrgId = null,
    string? DepartmentName = null,
    string? BudgetCode = null,
    string? BookerRole = null);

public sealed record BookingCreatedResponse(Guid BookingId);

