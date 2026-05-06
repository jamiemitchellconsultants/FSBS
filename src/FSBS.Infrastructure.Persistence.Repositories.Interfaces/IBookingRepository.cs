using FSBS.Shared.Bookings;
using FSBS.Shared.Common;

namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

/// <summary>
/// Read-side booking repository used by query handlers to return DTOs directly
/// to the API without going through the domain aggregate.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Returns a cursor-paginated list of the given user's bookings, sorted
    /// newest-slot-first.
    /// </summary>
    Task<PagedResult<BookingSummaryDto>> GetMyBookingsPageAsync(
        Guid userId, string? afterCursor, int limit, CancellationToken ct = default);

    /// <summary>
    /// Returns all bookings for the given user whose slots fall within the
    /// half-open interval [<paramref name="from"/>, <paramref name="to"/>].
    /// </summary>
    Task<IReadOnlyList<BookingSummaryDto>> GetMyBookingsForRangeAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail DTO for a single booking that is owned by
    /// <paramref name="userId"/>, or null when not found or not owned.
    /// </summary>
    Task<BookingDetailDto?> GetMyBookingDetailAsync(
        Guid bookingId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all bookings currently in PendingApproval status across all
    /// tenants, for display in the SalesStaff approval queue.
    /// </summary>
    Task<IReadOnlyList<BookingSummaryDto>> GetPendingApprovalAsync(
        CancellationToken ct = default);
}
