using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public sealed class GetMyBookingsHandler(IBookingRepository bookings, ICurrentUser currentUser)
    : IRequestHandler<GetMyBookingsQuery, PagedResult<BookingSummaryDto>>
{
    public Task<PagedResult<BookingSummaryDto>> Handle(GetMyBookingsQuery request, CancellationToken ct) =>
        bookings.GetMyBookingsPageAsync(currentUser.UserId, request.AfterCursor, request.Limit, ct);
}
