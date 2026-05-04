using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public record GetMyBookingsForRangeQuery(DateTimeOffset From, DateTimeOffset To)
    : IRequest<IReadOnlyList<BookingSummaryDto>>;
