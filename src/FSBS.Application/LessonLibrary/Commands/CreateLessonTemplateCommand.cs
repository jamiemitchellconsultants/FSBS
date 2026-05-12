using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Creates a new lesson template in the calling user's tenant curriculum
/// library. Requires <c>SystemAdmin</c> or <c>CourseDirector</c>.
/// </summary>
public record CreateLessonTemplateCommand(
    string Title,
    string? Description,
    TrainingType TrainingType,
    int DefaultMinDurationMins,
    bool RequiresInstructor,
    bool IsMandatoryByDefault,
    string? Category) : ICommand<LessonTemplateDto>;
