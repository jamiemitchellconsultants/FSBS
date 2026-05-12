namespace FSBS.Application.Common.Exceptions;

/// <summary>
/// Thrown when an attempt is made to attach an inactive (retired) or
/// soft-deleted lesson template to a module.
/// </summary>
public sealed class LessonTemplateInactiveException(Guid lessonTemplateId)
    : Exception($"Lesson template '{lessonTemplateId}' is not active and cannot be attached.");
