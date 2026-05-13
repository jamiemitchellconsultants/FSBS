using FSBS.Domain.Enums;

namespace FSBS.Shared.Courses;

/// <summary>Response body for <c>POST /v1/courses</c>.</summary>
public record CreateCourseResponse(
    Guid CourseId,
    string Title,
    TrainingType TrainingType,
    int ModuleCount);
