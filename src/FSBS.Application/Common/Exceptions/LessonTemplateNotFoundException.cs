namespace FSBS.Application.Common.Exceptions;

/// <summary>Thrown when a lesson template lookup yields no row for the current tenant.</summary>
public sealed class LessonTemplateNotFoundException(Guid lessonTemplateId)
    : Exception($"Lesson template '{lessonTemplateId}' was not found.");
