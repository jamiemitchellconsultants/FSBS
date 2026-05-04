using FSBS.Domain.Enums;

namespace FSBS.Domain.Exceptions;

/// <summary>
/// Thrown when a command attempts a booking state transition that is not
/// permitted by the state machine (e.g. approving a Confirmed booking).
/// </summary>
public sealed class InvalidBookingStateTransitionException(
    Guid bookingId,
    BookingStatus from,
    BookingStatus to)
    : DomainException($"Booking {bookingId} cannot transition from {from} to {to}.");
