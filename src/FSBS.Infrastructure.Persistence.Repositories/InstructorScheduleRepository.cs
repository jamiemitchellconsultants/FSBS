using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

public sealed class InstructorScheduleRepository(FsbsDbContext db) : IInstructorScheduleRepository
{
    public Task<Guid?> GetInstructorIdForUserAsync(Guid userId, CancellationToken ct = default) =>
        db.Instructors.AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => (Guid?)i.Id)
            .FirstOrDefaultAsync(ct);

    public Task<InstructorWeeklyPattern?> GetActivePatternAsync(Guid instructorId, CancellationToken ct = default) =>
        db.InstructorWeeklyPatterns
            .Include(p => p.Slots)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.InstructorId == instructorId && p.EffectiveTo == null, ct);

    public async Task<InstructorWeeklyPattern> ReplaceActivePatternAsync(
        Guid instructorId,
        IReadOnlyList<(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)> slots,
        DateOnly asOf,
        CancellationToken ct = default)
    {
        // Close any currently-open pattern.
        var open = await db.InstructorWeeklyPatterns
            .Include(p => p.Slots)
            .Where(p => p.InstructorId == instructorId && p.EffectiveTo == null)
            .FirstOrDefaultAsync(ct);

        if (open is not null)
        {
            // If the existing open pattern starts today, replace it in place
            // (avoids stacking same-day patterns and avoids tripping the
            // partial-unique index momentarily).
            if (open.EffectiveFrom == asOf)
            {
                db.InstructorWeeklyPatternSlots.RemoveRange(open.Slots);
                open.Slots.Clear();
                foreach (var s in slots)
                    open.Slots.Add(new InstructorWeeklyPatternSlot
                    {
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                    });
                await db.SaveChangesAsync(ct);
                return open;
            }

            open.EffectiveTo = asOf;
            await db.SaveChangesAsync(ct);
        }

        var fresh = new InstructorWeeklyPattern
        {
            InstructorId = instructorId,
            EffectiveFrom = asOf,
            EffectiveTo = null,
            Slots = slots.Select(s => new InstructorWeeklyPatternSlot
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
            }).ToList(),
        };

        db.InstructorWeeklyPatterns.Add(fresh);
        await db.SaveChangesAsync(ct);
        return fresh;
    }

    public async Task<IReadOnlyList<InstructorAvailability>> GetOverridesAsync(
        Guid instructorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct = default) =>
        await db.InstructorAvailabilities
            .AsNoTracking()
            .Where(a => a.InstructorId == instructorId
                        && a.StartAt < toUtc
                        && a.EndAt > fromUtc)
            .OrderBy(a => a.StartAt)
            .ToListAsync(ct);

    public async Task<InstructorAvailability> AddOverrideAsync(
        Guid instructorId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        AvailabilityType type,
        string? notes,
        CancellationToken ct = default)
    {
        var entity = new InstructorAvailability
        {
            InstructorId = instructorId,
            StartAt = startAtUtc,
            EndAt = endAtUtc,
            AvailabilityType = type,
            Notes = notes,
        };
        db.InstructorAvailabilities.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<InstructorAvailability> UpdateOverrideAsync(
        Guid instructorId,
        Guid overrideId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        AvailabilityType type,
        string? notes,
        CancellationToken ct = default)
    {
        var entity = await db.InstructorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == overrideId && a.InstructorId == instructorId, ct)
            ?? throw new KeyNotFoundException($"Availability override {overrideId} not found for instructor {instructorId}.");

        entity.StartAt = startAtUtc;
        entity.EndAt = endAtUtc;
        entity.AvailabilityType = type;
        entity.Notes = notes;
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteOverrideAsync(Guid instructorId, Guid overrideId, CancellationToken ct = default)
    {
        var entity = await db.InstructorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == overrideId && a.InstructorId == instructorId, ct);

        if (entity is null) return;

        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReplaceDayAvailableOverridesAsync(
        Guid instructorId,
        DateOnly date,
        IReadOnlyList<(TimeOnly StartTime, TimeOnly EndTime)> available,
        TimeZoneInfo schoolTimeZone,
        CancellationToken ct = default)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, schoolTimeZone), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, schoolTimeZone), TimeSpan.Zero);

        // Soft-delete every Available override that overlaps this day.
        var existing = await db.InstructorAvailabilities
            .Where(a => a.InstructorId == instructorId
                        && a.AvailabilityType == AvailabilityType.Available
                        && a.StartAt < endUtc
                        && a.EndAt > startUtc)
            .ToListAsync(ct);

        foreach (var e in existing)
            e.IsDeleted = true;

        // Insert the new ranges.
        foreach (var range in available)
        {
            var rangeStart = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(range.StartTime), schoolTimeZone);
            var rangeEnd = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(range.EndTime), schoolTimeZone);
            db.InstructorAvailabilities.Add(new InstructorAvailability
            {
                InstructorId = instructorId,
                StartAt = new DateTimeOffset(rangeStart, TimeSpan.Zero),
                EndAt = new DateTimeOffset(rangeEnd, TimeSpan.Zero),
                AvailabilityType = AvailabilityType.Available,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
