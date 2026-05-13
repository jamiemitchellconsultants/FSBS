using FSBS.Domain.Enums;

namespace FSBS.Application.Courses.Commands;

/// <summary>
/// Returned after a course (and its initial modules) has been persisted.
/// Mirrors <c>CreateCourseResponse</c> on the API layer.
/// </summary>
public record CreateCourseResult(
    Guid CourseId,
    string Title,
    TrainingType TrainingType,
    int ModuleCount);
