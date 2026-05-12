using FSBS.Domain.Enums;

namespace FSBS.Shared.LessonLibrary;

/// <summary>
/// Full detail of a lesson template, including the current usage count of
/// attached <c>Lessons</c>. Returned by <c>GET /v1/lesson-templates/{id}</c>
/// and by the create / update / set-active endpoints.
/// </summary>
public record LessonTemplateDto(
    Guid Id,
    string Title,
    string? Description,
    TrainingType TrainingType,
    int DefaultMinDurationMins,
    bool RequiresInstructor,
    bool IsMandatoryByDefault,
    string? Category,
    bool IsActive,
    int UsageCount);
