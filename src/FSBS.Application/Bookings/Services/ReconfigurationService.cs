using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;

namespace FSBS.Application.Bookings.Services;

public sealed class ReconfigurationService(
    ISimulatorRepository simulatorRepository,
    IBookingRepository bookingRepository,
    IReconfigurationTemplateRepository templateRepository,
    IReconfigurationSlotRepository slotRepository) : IReconfigurationService
{
    public async Task<ReconfigurationSlot?> BuildSlotForConfirmedBookingAsync(
        Booking confirmedBooking,
        BookingSlot confirmedSlot,
        CancellationToken ct = default)
    {
        var bay = await simulatorRepository.FindBayAsync(confirmedSlot.BayId, ct)
            ?? throw new InvalidOperationException($"Bay {confirmedSlot.BayId} not found.");

        var unit = await simulatorRepository.FindByIdAsync(bay.SimulatorUnitId, ct)
            ?? throw new InvalidOperationException($"SimulatorUnit {bay.SimulatorUnitId} not found.");

        var fromConfigId = confirmedBooking.ConfigId;
        var slotEnd = confirmedSlot.EndAt;

        var nextSlot = await bookingRepository.FindNextSlotOnBayAsync(
            confirmedSlot.BayId, slotEnd, ct: ct);

        Guid toConfigId;

        if (nextSlot is not null)
        {
            var nextBooking = await bookingRepository.FindByIdAsync(nextSlot.BookingId, ct)
                ?? throw new InvalidOperationException($"Booking {nextSlot.BookingId} not found.");

            if (nextBooking.ConfigId == fromConfigId)
                return null;
            toConfigId = nextBooking.ConfigId;
        }
        else
        {
            toConfigId = unit.ActiveConfigurationId ?? fromConfigId;
        }

        var durationMins = await ResolveDurationAsync(fromConfigId, toConfigId, unit, ct);

        return new ReconfigurationSlot
        {
            BayId = confirmedSlot.BayId,
            PrecedingBookingId = confirmedBooking.Id,
            StartAt = slotEnd,
            EndAt = slotEnd.AddMinutes(durationMins),
            DurationMins = durationMins
        };
    }

    public async Task RemoveOrphanedSlotsAsync(
        Booking cancelledBooking,
        CancellationToken ct = default)
    {
        var cancelledSlot = cancelledBooking.Slots.FirstOrDefault();
        if (cancelledSlot is null)
            return;

        // Remove the direct reconfig slot that was attached to the cancelled booking.
        var directSlot = await slotRepository.FindByPrecedingBookingAsync(cancelledBooking.Id, ct);
        if (directSlot is not null)
            slotRepository.Remove(directSlot);

        // Re-evaluate the preceding booking: its reconfig slot may now point at
        // the wrong target config because the booking it was aimed at is gone.
        var precedingSlot = await bookingRepository.FindPrecedingSlotOnBayAsync(
            cancelledSlot.BayId,
            cancelledSlot.StartAt,
            excludeBookingId: cancelledBooking.Id,
            ct: ct);

        if (precedingSlot is null)
            return;

        var precedingBooking = await bookingRepository.FindByIdAsync(precedingSlot.BookingId, ct);
        if (precedingBooking is null)
            return;

        // Drop the preceding booking's current reconfig slot so we can replace it.
        var precedingReconfig = await slotRepository.FindByPrecedingBookingAsync(precedingBooking.Id, ct);
        if (precedingReconfig is not null)
            slotRepository.Remove(precedingReconfig);

        // Rebuild: the preceding booking should now reconfig toward whatever
        // follows the cancelled booking (or toward the active config if nothing follows).
        var rebuilt = await BuildSlotForConfirmedBookingAsync(
            precedingBooking,
            precedingSlot,
            ct);

        if (rebuilt is not null)
            await slotRepository.AddAsync(rebuilt, ct);
    }

    private async Task<int> ResolveDurationAsync(
        Guid fromConfigId,
        Guid toConfigId,
        SimulatorUnit unit,
        CancellationToken ct)
    {
        if (fromConfigId == toConfigId)
            return unit.DefaultReconfigMins;

        var template = await templateRepository.FindAsync(fromConfigId, toConfigId, ct);
        return template?.DurationMins ?? unit.DefaultReconfigMins;
    }
}
