using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Shared.InstructorSchedule;

namespace FSBS.Application.InstructorSchedule.Services;

/// <summary>
/// Pure projection of a weekly pattern + concrete overrides into the list of
/// effective UTC intervals to render on the calendar. No I/O — feed it the
/// data you already loaded and it does the layering.
/// </summary>
/// <remarks>
/// Layering rules:
/// <list type="bullet">
///   <item>The weekly pattern is materialised onto each date in [from, to] using
///         the school time zone, producing baseline <see cref="EffectiveIntervalSource.Pattern"/> intervals.</item>
///   <item><see cref="AvailabilityType.Available"/> overrides are added as
///         <see cref="EffectiveIntervalSource.Override"/> intervals (not merged with pattern coverage).</item>
///   <item><see cref="AvailabilityType.Leave"/> and <see cref="AvailabilityType.Other"/>
///         overrides clip pattern coverage that intersects them; clipped fragments
///         are emitted as <see cref="EffectiveIntervalSource.PatternClipped"/>.</item>
/// </list>
/// The Leave / Other windows themselves are returned to the UI via the raw
/// <c>Overrides</c> list so they can be drawn distinctly — they are not in the
/// effective-intervals list because that list represents working time only.
/// </remarks>
public static class InstructorScheduleResolver
{
    /// <summary>The school operates in UK local time.</summary>
    public static readonly TimeZoneInfo SchoolTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static IReadOnlyList<EffectiveIntervalDto> Resolve(
        InstructorWeeklyPattern? pattern,
        IReadOnlyList<InstructorAvailability> overrides,
        DateOnly from,
        DateOnly to,
        TimeZoneInfo? timeZone = null)
    {
        var tz = timeZone ?? SchoolTimeZone;
        var result = new List<(DateTimeOffset Start, DateTimeOffset End, AvailabilityType Type, EffectiveIntervalSource Source)>();

        // 1. Materialise pattern slots onto every date in the window.
        var patternIntervals = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        if (pattern is not null)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (pattern.EffectiveFrom > date) continue;
                if (pattern.EffectiveTo is { } endExclusive && date >= endExclusive) continue;

                foreach (var slot in pattern.Slots.Where(s => s.DayOfWeek == date.DayOfWeek))
                {
                    var localStart = date.ToDateTime(slot.StartTime);
                    var localEnd = slot.EndTime > slot.StartTime
                        ? date.ToDateTime(slot.EndTime)
                        : date.AddDays(1).ToDateTime(slot.EndTime); // unused given DB constraint, kept for safety
                    var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
                    var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
                    patternIntervals.Add((new DateTimeOffset(startUtc, TimeSpan.Zero), new DateTimeOffset(endUtc, TimeSpan.Zero)));
                }
            }
        }

        // 2. Subtract Leave / Other overrides from the pattern intervals.
        var subtractors = overrides
            .Where(o => o.AvailabilityType is AvailabilityType.Leave or AvailabilityType.Other)
            .Select(o => (Start: o.StartAt, End: o.EndAt))
            .ToList();

        var clippedPattern = SubtractRanges(patternIntervals, subtractors);

        foreach (var (start, end, wasClipped) in clippedPattern)
        {
            result.Add((start, end, AvailabilityType.Available,
                wasClipped ? EffectiveIntervalSource.PatternClipped : EffectiveIntervalSource.Pattern));
        }

        // 3. Add Available overrides as their own intervals (not merged with pattern coverage).
        foreach (var ov in overrides.Where(o => o.AvailabilityType == AvailabilityType.Available))
        {
            result.Add((ov.StartAt, ov.EndAt, AvailabilityType.Available, EffectiveIntervalSource.Override));
        }

        return result
            .OrderBy(r => r.Start)
            .Select(r => new EffectiveIntervalDto(r.Start, r.End, r.Type.ToString(), r.Source))
            .ToList();
    }

    /// <summary>
    /// Returns the original intervals with the subtracting ranges removed. The
    /// boolean indicates whether the surviving fragment was the result of a clip
    /// (any portion of the original was removed).
    /// </summary>
    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End, bool WasClipped)> SubtractRanges(
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> originals,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> subtractors)
    {
        foreach (var orig in originals)
        {
            var pieces = new List<(DateTimeOffset Start, DateTimeOffset End)> { orig };
            var clipped = false;

            foreach (var sub in subtractors)
            {
                var next = new List<(DateTimeOffset Start, DateTimeOffset End)>();
                foreach (var p in pieces)
                {
                    if (sub.End <= p.Start || sub.Start >= p.End)
                    {
                        next.Add(p);
                        continue;
                    }
                    clipped = true;
                    if (sub.Start > p.Start) next.Add((p.Start, sub.Start));
                    if (sub.End < p.End) next.Add((sub.End, p.End));
                }
                pieces = next;
                if (pieces.Count == 0) break;
            }

            foreach (var piece in pieces)
                yield return (piece.Start, piece.End, clipped);
        }
    }
}
