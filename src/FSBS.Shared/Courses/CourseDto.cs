using FSBS.Domain.Enums;

namespace FSBS.Shared.Courses;

/// <summary>
/// Read-side projection of a course. Defined now so the future
/// <c>GET /v1/courses/{id}</c> read slice has a stable shape that the
/// front-end can already target.
/// </summary>
public record CourseDto(
    Guid Id,
    string Title,
    string? Description,
    string? RegulatoryFramework,
    decimal TotalHours,
    TrainingType TrainingType,
    bool IsActive,
    int ModuleCount);
