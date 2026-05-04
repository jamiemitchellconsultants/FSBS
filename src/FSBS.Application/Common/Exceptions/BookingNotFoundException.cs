namespace FSBS.Application.Common.Exceptions;

public sealed class BookingNotFoundException(Guid bookingId)
    : Exception($"Booking {bookingId} was not found or you do not have permission to view it.");
