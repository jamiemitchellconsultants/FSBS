using FSBS.Domain.Enums;

namespace FSBS.Shared.LessonLibrary;

/// <summary>Request body for <c>POST /v1/lesson-templates</c>.</summary>
public record CreateLessonTemplateRequest(
    string Title,
    string? Description,
    TrainingType TrainingType,
    int DefaultMinDurationMins,
    bool RequiresInstructor,
    bool IsMandatoryByDefault,
    string? Category);
