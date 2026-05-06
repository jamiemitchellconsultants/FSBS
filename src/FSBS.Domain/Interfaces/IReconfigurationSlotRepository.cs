using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

public interface IReconfigurationSlotRepository
{
    /// <summary>
    /// Returns the reconfiguration slot whose <c>PrecedingBookingId</c> matches
    /// the given booking. Returns null if the booking had no reconfig slot attached.
    /// </summary>
    Task<ReconfigurationSlot?> FindByPrecedingBookingAsync(
        Guid precedingBookingId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true if any reconfiguration slot on the bay overlaps the given
    /// half-open interval [start, end). Used to prevent bookings from being placed
    /// inside an existing reconfiguration window.
    /// </summary>
    Task<bool> HasOverlapAsync(
        Guid bayId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default);

    /// <summary>Adds the reconfiguration slot to the change tracker for insertion on next SaveChanges.</summary>
    Task AddAsync(ReconfigurationSlot slot, CancellationToken ct = default);

    /// <summary>
    /// Marks the slot for hard deletion. Reconfig slots are never soft-deleted;
    /// the change is flushed when IUnitOfWork.SaveChangesAsync is called.
    /// </summary>
    void Remove(ReconfigurationSlot slot);
}
