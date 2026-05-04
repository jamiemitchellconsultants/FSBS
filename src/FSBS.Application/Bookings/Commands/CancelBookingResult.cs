using FSBS.Domain.Enums;

namespace FSBS.Application.Bookings.Commands;

public record CancelBookingResult(Guid BookingId, BookingStatus Status);
