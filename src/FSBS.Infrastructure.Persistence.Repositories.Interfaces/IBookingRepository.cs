using FSBS.Shared.Bookings;
using FSBS.Shared.Common;

namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<PagedResult<BookingSummaryDto>> GetMyBookingsPageAsync(
        Guid userId, string? afterCursor, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<BookingSummaryDto>> GetMyBookingsForRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    Task<BookingDetailDto?> GetMyBookingDetailAsync(
        Guid bookingId, Guid userId, CancellationToken ct = default);
}
