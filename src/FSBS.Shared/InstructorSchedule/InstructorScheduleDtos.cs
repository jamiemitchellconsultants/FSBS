namespace FSBS.Shared.InstructorSchedule;

/// <summary>One working interval inside a recurring weekly pattern.</summary>
public sealed record WeeklyPatternSlotDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

/// <summary>The currently-effective weekly pattern for an instructor.</summary>
public sealed record WeeklyPatternDto(Guid Id, DateOnly EffectiveFrom, IReadOnlyList<WeeklyPatternSlotDto> Slots);

/// <summary>
/// A concrete UTC override layered on top of the weekly pattern.
/// <see cref="Type"/> is one of <c>"Available"</c>, <c>"Leave"</c>, or <c>"Other"</c>
/// (mirrors <c>FSBS.Domain.Enums.AvailabilityType</c>; kept as a string here so
/// <c>FSBS.Shared</c> stays Domain-free).
/// </summary>
public sealed record AvailabilityOverrideDto(
    Guid Id,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Type,
    string? Notes);

/// <summary>Where an effective interval came from.</summary>
public enum EffectiveIntervalSource
{
    /// <summary>Materialised from the active weekly pattern.</summary>
    Pattern,

    /// <summary>An explicit Available concrete override.</summary>
    Override,

    /// <summary>Pattern coverage that was clipped by a Leave/Other override.</summary>
    PatternClipped,
}

/// <summary>A computed (UTC) interval for the calendar to render.</summary>
public sealed record EffectiveIntervalDto(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Type,
    EffectiveIntervalSource Source);

/// <summary>The full schedule payload returned to the calendar UI for a date window.</summary>
public sealed record InstructorScheduleDto(
    Guid InstructorId,
    DateOnly From,
    DateOnly To,
    WeeklyPatternDto? Pattern,
    IReadOnlyList<AvailabilityOverrideDto> Overrides,
    IReadOnlyList<EffectiveIntervalDto> EffectiveIntervals);

/// <summary>Body for replacing the active weekly pattern.</summary>
public sealed record WeeklyPatternUpsertRequest(IReadOnlyList<WeeklyPatternSlotDto> Slots);

/// <summary>A single naive time range used inside <see cref="SingleDayUpsertRequest"/>.</summary>
public sealed record TimeRangeDto(TimeOnly StartTime, TimeOnly EndTime);

/// <summary>Body for replacing the concrete Available overrides for one calendar day.</summary>
public sealed record SingleDayUpsertRequest(IReadOnlyList<TimeRangeDto> Available);

/// <summary>
/// Body for creating or updating a single override. <see cref="Type"/> must be
/// one of <c>"Available"</c>, <c>"Leave"</c>, or <c>"Other"</c>.
/// </summary>
public sealed record OverrideRequest(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Type,
    string? Notes);
