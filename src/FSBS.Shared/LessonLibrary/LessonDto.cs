namespace FSBS.Shared.LessonLibrary;

/// <summary>
/// Projection of a <c>Lesson</c> row returned by the "attach from template"
/// endpoint. A full <c>Lessons</c>-feature DTO will replace this when that
/// slice ships.
/// </summary>
public record LessonDto(
    Guid Id,
    Guid ModuleId,
    string Title,
    int SequenceOrder,
    int MinDurationMins,
    bool RequiresInstructor,
    bool IsMandatory,
    Guid? SourceTemplateId);
