namespace FSBS.Application.Bookings.Commands;

public record ApproveBookingResult(
    Guid BookingId,
    decimal GrossPriceGbp,
    decimal DiscountGbp,
    decimal NetPriceGbp);
