namespace FSBS.Shared.LessonLibrary;

/// <summary>
/// Request body for <c>POST /v1/modules/{moduleId}/lessons/from-template</c>.
/// Override fields default to the template's own values when left null.
/// </summary>
public record AttachLessonToModuleRequest(
    Guid LessonTemplateId,
    int SequenceOrder,
    int? MinDurationMins,
    bool? RequiresInstructor,
    bool? IsMandatory);
