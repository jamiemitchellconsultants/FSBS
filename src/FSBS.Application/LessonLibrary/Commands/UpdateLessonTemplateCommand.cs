using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Edits an existing lesson template. The change does not propagate to
/// previously-attached <c>Lessons</c>; their fields remain at the values copied
/// at attach time.
/// </summary>
public record UpdateLessonTemplateCommand(
    Guid Id,
    string Title,
    string? Description,
    TrainingType TrainingType,
    int DefaultMinDurationMins,
    bool RequiresInstructor,
    bool IsMandatoryByDefault,
    string? Category) : ICommand<LessonTemplateDto>;
