using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public sealed class GetMyBookingsForRangeHandler(IBookingRepository bookings, ICurrentUser currentUser)
    : IRequestHandler<GetMyBookingsForRangeQuery, IReadOnlyList<BookingSummaryDto>>
{
    public Task<IReadOnlyList<BookingSummaryDto>> Handle(GetMyBookingsForRangeQuery request, CancellationToken ct) =>
        bookings.GetMyBookingsForRangeAsync(currentUser.UserId, request.From, request.To, ct);
}
