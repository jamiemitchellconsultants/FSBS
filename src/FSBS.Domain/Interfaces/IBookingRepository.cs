using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the Booking aggregate. Used by command handlers.
/// Query-side operations (DTOs, pagination) live in the Infrastructure read model.
/// </summary>
public interface IBookingRepository
{
    /// <summary>Returns the booking with its Slots collection loaded.</summary>
    Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Finds a booking by its idempotency key. Returns null if no booking has
    /// been created with that key, allowing the handler to safely create one.
    /// </summary>
    Task<Booking?> FindByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-cancelled booking slots that overlap the given range on
    /// the specified bay. Used by the booking handler to detect conflicts before
    /// creating a new slot.
    /// </summary>
    Task<IReadOnlyList<BookingSlot>> FindConflictingSlotsAsync(
        Guid bayId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the earliest non-cancelled booking slot on the bay that starts at
    /// or after <paramref name="afterTime"/>, with its parent Booking loaded.
    /// <paramref name="excludeBookingId"/> is omitted from results so that a
    /// booking being cancelled within the same transaction is not returned.
    /// </summary>
    Task<BookingSlot?> FindNextSlotOnBayAsync(
        Guid bayId,
        DateTimeOffset afterTime,
        Guid? excludeBookingId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the latest non-cancelled booking slot on the bay whose EndAt is
    /// at or before <paramref name="beforeTime"/>, with its parent Booking loaded.
    /// <paramref name="excludeBookingId"/> is omitted from results for the same
    /// reason as <see cref="FindNextSlotOnBayAsync"/>.
    /// </summary>
    Task<BookingSlot?> FindPrecedingSlotOnBayAsync(
        Guid bayId,
        DateTimeOffset beforeTime,
        Guid? excludeBookingId = null,
        CancellationToken ct = default);

    Task AddAsync(Booking booking, CancellationToken ct = default);
}
