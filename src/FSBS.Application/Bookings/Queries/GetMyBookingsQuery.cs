using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

/// <summary>Returns the current user's bookings, cursor-paginated newest-first.</summary>
public record GetMyBookingsQuery(string? AfterCursor, int Limit = 20)
    : IRequest<PagedResult<BookingSummaryDto>>;
