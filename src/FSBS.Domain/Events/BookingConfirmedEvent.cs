namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a booking transitions to Confirmed and the price is locked.
/// Triggers the confirmation email and, for corporate bookings, an account
/// balance check.
/// </summary>
public record BookingConfirmedEvent(
    Guid BookingId,
    Guid BookedBy,
    Guid? OrgId,
    decimal GrossPriceGbp,
    decimal DiscountPct,
    decimal NetPriceGbp) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
