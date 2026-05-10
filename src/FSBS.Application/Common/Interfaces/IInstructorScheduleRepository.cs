using FSBS.Domain.Entities;
using FSBS.Domain.Enums;

namespace FSBS.Application.Common.Interfaces;

/// <summary>
/// Combined read- and write-side repository for instructor schedules. Pattern
/// data and concrete <see cref="InstructorAvailability"/> overrides are stored
/// in different tables but always queried and mutated together for a given
/// instructor, so a single interface keeps the call sites tidy.
/// </summary>
public interface IInstructorScheduleRepository
{
    /// <summary>
    /// Resolves the <see cref="Instructor.Id"/> for a given <see cref="AppUser.Id"/>,
    /// or <c>null</c> if the user is not an instructor.
    /// </summary>
    Task<Guid?> GetInstructorIdForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the currently-open pattern for the instructor (the row with
    /// <c>effective_to IS NULL</c>), with its slots eager-loaded. Returns
    /// <c>null</c> if no pattern has been declared yet.
    /// </summary>
    Task<InstructorWeeklyPattern?> GetActivePatternAsync(Guid instructorId, CancellationToken ct = default);

    /// <summary>
    /// Atomically supersedes the instructor's active pattern. Closes the existing
    /// open pattern (sets <c>EffectiveTo = asOf</c>) and inserts a fresh pattern
    /// with the supplied slots and <c>EffectiveFrom = asOf</c>. The returned
    /// entity is the newly-active pattern.
    /// </summary>
    Task<InstructorWeeklyPattern> ReplaceActivePatternAsync(
        Guid instructorId,
        IReadOnlyList<(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)> slots,
        DateOnly asOf,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the instructor's concrete overrides whose [StartAt, EndAt) range
    /// intersects the supplied UTC window. Includes Available extras, Leave, and Other.
    /// </summary>
    Task<IReadOnlyList<InstructorAvailability>> GetOverridesAsync(
        Guid instructorId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken ct = default);

    /// <summary>Creates a new concrete override and returns it.</summary>
    Task<InstructorAvailability> AddOverrideAsync(
        Guid instructorId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        AvailabilityType type,
        string? notes,
        CancellationToken ct = default);

    /// <summary>Updates an existing override that belongs to the given instructor.</summary>
    Task<InstructorAvailability> UpdateOverrideAsync(
        Guid instructorId,
        Guid overrideId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        AvailabilityType type,
        string? notes,
        CancellationToken ct = default);

    /// <summary>Soft-deletes an override that belongs to the given instructor.</summary>
    Task DeleteOverrideAsync(Guid instructorId, Guid overrideId, CancellationToken ct = default);

    /// <summary>
    /// Replaces the concrete <see cref="AvailabilityType.Available"/> overrides
    /// that fall on the given local-time date with the supplied ranges. Leave/Other
    /// overrides on the same date are untouched. Used by "Set this day…".
    /// </summary>
    Task ReplaceDayAvailableOverridesAsync(
        Guid instructorId,
        DateOnly date,
        IReadOnlyList<(TimeOnly StartTime, TimeOnly EndTime)> available,
        TimeZoneInfo schoolTimeZone,
        CancellationToken ct = default);
}
