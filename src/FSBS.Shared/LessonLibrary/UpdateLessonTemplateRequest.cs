using FSBS.Domain.Enums;

namespace FSBS.Shared.LessonLibrary;

/// <summary>Request body for <c>PUT /v1/lesson-templates/{id}</c>.</summary>
public record UpdateLessonTemplateRequest(
    string Title,
    string? Description,
    TrainingType TrainingType,
    int DefaultMinDurationMins,
    bool RequiresInstructor,
    bool IsMandatoryByDefault,
    string? Category);
