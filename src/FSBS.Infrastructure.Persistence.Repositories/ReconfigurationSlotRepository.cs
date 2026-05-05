using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class ReconfigurationSlotRepository(FsbsDbContext db) : IReconfigurationSlotRepository
{
    public Task<ReconfigurationSlot?> FindByPrecedingBookingAsync(
        Guid precedingBookingId,
        CancellationToken ct = default) =>
        db.ReconfigurationSlots
            .FirstOrDefaultAsync(s => s.PrecedingBookingId == precedingBookingId, ct);

    public Task<bool> HasOverlapAsync(
        Guid bayId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default) =>
        db.ReconfigurationSlots
            .AnyAsync(
                s => s.BayId   == bayId
                  && s.StartAt <  end
                  && s.EndAt   >  start,
                ct);

    public async Task AddAsync(ReconfigurationSlot slot, CancellationToken ct = default)
    {
        db.ReconfigurationSlots.Add(slot);
        await Task.CompletedTask;
    }

    public void Remove(ReconfigurationSlot slot) =>
        db.ReconfigurationSlots.Remove(slot);
}
