namespace FSBS.Domain.ValueObjects;

/// <summary>
/// A half-open interval [Start, End) representing a contiguous block of time.
/// Used for booking slots, reconfiguration windows, and maintenance windows.
/// </summary>
public record DateTimeRange
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public DateTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after Start.", nameof(end));

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;
    public int DurationMins => (int)Duration.TotalMinutes;

    /// <summary>Returns true if this range shares any time with <paramref name="other"/>.</summary>
    public bool Overlaps(DateTimeRange other) => Start < other.End && End > other.Start;

    /// <summary>Returns true if <paramref name="point"/> falls within this range.</summary>
    public bool Contains(DateTimeOffset point) => point >= Start && point < End;

    /// <summary>Returns true if <paramref name="other"/> falls entirely within this range.</summary>
    public bool Contains(DateTimeRange other) => Start <= other.Start && End >= other.End;
}
