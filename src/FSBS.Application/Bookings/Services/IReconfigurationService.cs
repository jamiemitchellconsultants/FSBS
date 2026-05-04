using FSBS.Domain.Entities;

namespace FSBS.Application.Bookings.Services;

public interface IReconfigurationService
{
    /// <summary>
    /// Builds the reconfiguration slot to be inserted immediately after a booking
    /// is confirmed. Returns null only when the immediately following booking on
    /// the same bay uses the same configuration (no turnaround required).
    /// In all other cases a slot is always returned — either transitioning to the
    /// next booking's config, or returning to the unit's active config for
    /// operational readiness when no following booking exists.
    /// </summary>
    Task<ReconfigurationSlot?> BuildSlotForConfirmedBookingAsync(
        Booking confirmedBooking,
        BookingSlot confirmedSlot,
        CancellationToken ct = default);

    /// <summary>
    /// Removes the reconfiguration slot attached to a cancelled or rejected
    /// booking, then re-evaluates the preceding booking's reconfig slot in case
    /// it now needs to point at a different following configuration.
    /// </summary>
    Task RemoveOrphanedSlotsAsync(
        Booking cancelledBooking,
        CancellationToken ct = default);
}
