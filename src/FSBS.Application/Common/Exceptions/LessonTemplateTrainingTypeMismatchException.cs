using FSBS.Domain.Enums;

namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Thrown when a lesson template's <see cref="TrainingType"/> does not match
/// the parent course's training type while attaching to a module.
/// </summary>
public sealed class LessonTemplateTrainingTypeMismatchException(
    Guid lessonTemplateId,
    TrainingType templateTrainingType,
    TrainingType courseTrainingType)
    : Exception(
        $"Lesson template '{lessonTemplateId}' has training type '{templateTrainingType}', " +
        $"which does not match the course training type '{courseTrainingType}'.");
