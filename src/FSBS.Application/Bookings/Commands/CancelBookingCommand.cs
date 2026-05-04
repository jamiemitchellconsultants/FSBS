using FSBS.Application.Common.Interfaces;

namespace FSBS.Application.Bookings.Commands;

/// <summary>
/// Cancels a booking. The resulting status — CancelledByCustomer or
/// CancelledByAdmin — is determined at runtime from ICurrentUser:
/// if the caller is the booker (or a corporate manager acting for their org)
/// the booking is marked CancelledByCustomer; staff roles produce CancelledByAdmin.
/// An optional reason may be supplied in either case.
/// </summary>
public record CancelBookingCommand(
    Guid BookingId,
    string? Reason = null) : ICommand<CancelBookingResult>;
