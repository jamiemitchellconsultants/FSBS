using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Write-side <see cref="IBookingRepository"/> used by command handlers that
/// load and mutate the Booking aggregate. Read-side DTO queries live in
/// <see cref="BookingRepository"/>; the two cleanly separate the aggregate
/// surface from the projection surface.
/// </summary>
internal sealed class BookingWriteRepository(FsbsDbContext db) : IBookingRepository
{
    public Task<Booking?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Bookings
            .Include(b => b.Slots)
            .Include(b => b.Approval)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<Booking?> FindByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default) =>
        db.Bookings
            .FirstOrDefaultAsync(b => b.IdempotencyKey == idempotencyKey, ct);

    public async Task<IReadOnlyList<BookingSlot>> FindConflictingSlotsAsync(
        Guid bayId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default) =>
        await db.BookingSlots
            .Where(s => s.BayId == bayId
                     && s.SlotStatus != SlotStatus.Cancelled
                     && s.StartAt < end
                     && s.EndAt > start)
            .ToListAsync(ct);

    public Task<BookingSlot?> FindNextSlotOnBayAsync(
        Guid bayId,
        DateTimeOffset afterTime,
        Guid? excludeBookingId = null,
        CancellationToken ct = default) =>
        db.BookingSlots
            .Include(s => s.Booking)
            .Where(s => s.BayId == bayId
                     && s.SlotStatus != SlotStatus.Cancelled
                     && s.StartAt >= afterTime
                     && (excludeBookingId == null || s.BookingId != excludeBookingId))
            .OrderBy(s => s.StartAt)
            .FirstOrDefaultAsync(ct);

    public Task<BookingSlot?> FindPrecedingSlotOnBayAsync(
        Guid bayId,
        DateTimeOffset beforeTime,
        Guid? excludeBookingId = null,
        CancellationToken ct = default) =>
        db.BookingSlots
            .Include(s => s.Booking)
            .Where(s => s.BayId == bayId
                     && s.SlotStatus != SlotStatus.Cancelled
                     && s.EndAt <= beforeTime
                     && (excludeBookingId == null || s.BookingId != excludeBookingId))
            .OrderByDescending(s => s.EndAt)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        db.Bookings.Add(booking);
        return Task.CompletedTask;
    }
}
