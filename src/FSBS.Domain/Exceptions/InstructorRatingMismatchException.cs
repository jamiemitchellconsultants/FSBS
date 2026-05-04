using FSBS.Domain.Enums;

namespace FSBS.Domain.Exceptions;

/// <summary>
/// Thrown when an instructor is assigned to a booking whose TrainingType is not
/// in the instructor's TrainingTypeRatings list.
/// </summary>
public sealed class InstructorRatingMismatchException(Guid instructorId, TrainingType requiredType)
    : DomainException(
        $"Instructor {instructorId} is not rated to deliver {requiredType} training.");
