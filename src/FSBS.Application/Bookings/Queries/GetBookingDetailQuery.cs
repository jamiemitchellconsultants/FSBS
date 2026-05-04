using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public record GetBookingDetailQuery(Guid BookingId) : IRequest<BookingDetailDto>;
