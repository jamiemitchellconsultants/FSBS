namespace FSBS.Web.Services;

public sealed class BookingService(HttpClient http)
{
    public Task<IReadOnlyList<object>> GetBookingsAsync(string? afterCursor = null, int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);

    public Task<object?> GetBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.FromResult<object?>(null);

    public Task<Guid> CreateBookingAsync(object command, CancellationToken ct = default) =>
        Task.FromResult(Guid.Empty);

    public Task ApproveBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task CancelBookingAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<object>> GetPendingApprovalsAsync(string? afterCursor = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<object>>([]);
}
