using FSBS.Domain.Enums;

namespace FSBS.Shared.Courses;

/// <summary>Request body for <c>POST /v1/courses</c>.</summary>
public record CreateCourseRequest(
    string Title,
    string? Description,
    string? RegulatoryFramework,
    decimal TotalHours,
    TrainingType TrainingType,
    bool IsActive,
    IReadOnlyList<CreateCourseModuleRequest> Modules);
