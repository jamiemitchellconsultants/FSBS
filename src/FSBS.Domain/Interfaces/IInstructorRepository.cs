using FSBS.Domain.Entities;
using FSBS.Domain.Enums;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Read/write repository for instructor records. Used by booking command
/// handlers to validate instructor rating eligibility.
/// </summary>
public interface IInstructorRepository
{
    /// <summary>
    /// Returns the instructor record for the given application user ID with the
    /// <c>User</c> navigation property loaded, or null if the user is not registered
    /// as an instructor.
    /// </summary>
    Task<Instructor?> FindByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns instructors whose <c>TrainingTypeRatings</c> include the given
    /// training type, for use in the booking wizard instructor selection step.
    /// </summary>
    Task<IReadOnlyList<Instructor>> ListRatedForAsync(
        TrainingType trainingType,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all active instructors, ordered by employee number.
    /// </summary>
    Task<IReadOnlyList<Instructor>> ListAllAsync(CancellationToken ct = default);
}
