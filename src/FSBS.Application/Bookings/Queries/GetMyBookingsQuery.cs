using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public record GetMyBookingsQuery(string? AfterCursor, int Limit = 20)
    : IRequest<PagedResult<BookingSummaryDto>>;
