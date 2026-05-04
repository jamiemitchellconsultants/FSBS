using FSBS.Domain.Entities;
using FSBS.Domain.Enums;

namespace FSBS.Domain.Interfaces;

public interface IInstructorRepository
{
    Task<Instructor?> FindByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns instructors whose <c>TrainingTypeRatings</c> include the given
    /// training type, for use in the booking wizard instructor selection step.
    /// </summary>
    Task<IReadOnlyList<Instructor>> ListRatedForAsync(
        TrainingType trainingType,
        CancellationToken ct = default);
}
