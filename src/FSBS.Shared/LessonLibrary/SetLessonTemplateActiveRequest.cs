namespace FSBS.Shared.LessonLibrary;

/// <summary>Request body for <c>PUT /v1/lesson-templates/{id}/active</c>.</summary>
public record SetLessonTemplateActiveRequest(bool IsActive);
