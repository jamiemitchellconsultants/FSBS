using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

/// <summary>Returns the full detail of a single booking owned by the current user.</summary>
public record GetBookingDetailQuery(Guid BookingId) : IRequest<BookingDetailDto>;
