using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

/// <summary>Returns all of the current user's bookings whose slots fall within the given UTC date/time range.</summary>
public record GetMyBookingsForRangeQuery(DateTimeOffset From, DateTimeOffset To)
    : IRequest<IReadOnlyList<BookingSummaryDto>>;
