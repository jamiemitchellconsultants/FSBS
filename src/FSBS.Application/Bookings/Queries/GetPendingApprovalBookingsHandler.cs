using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public sealed class GetPendingApprovalBookingsHandler(IBookingRepository bookings)
    : IRequestHandler<GetPendingApprovalBookingsQuery, IReadOnlyList<BookingSummaryDto>>
{
    public Task<IReadOnlyList<BookingSummaryDto>> Handle(
        GetPendingApprovalBookingsQuery request, CancellationToken ct) =>
        bookings.GetPendingApprovalAsync(ct);
}
