using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Bookings;
using MediatR;

namespace FSBS.Application.Bookings.Queries;

public sealed class GetBookingDetailHandler(IBookingRepository bookings, ICurrentUser currentUser)
    : IRequestHandler<GetBookingDetailQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingDetailQuery request, CancellationToken ct)
    {
        var dto = await bookings.GetMyBookingDetailAsync(request.BookingId, currentUser.UserId, ct);
        return dto ?? throw new BookingNotFoundException(request.BookingId);
    }
}
