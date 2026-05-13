using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;

namespace FSBS.Application.Courses.Commands;

/// <summary>
/// Module input embedded in <see cref="CreateCourseCommand"/>. A new
/// <c>Module</c> row is inserted for each entry in the order supplied; the
/// <see cref="SequenceOrder"/> determines its position within the course and
/// must be unique within the request (validated by
/// <see cref="CreateCourseCommandValidator"/>).
/// </summary>
public record CreateCourseModuleInput(
    string Title,
    int SequenceOrder,
    string? Description);

/// <summary>
/// Creates a new <c>Course</c> together with its initial ordered
/// <c>Modules</c>. Issued by a <c>CourseDirector</c> or <c>SystemAdmin</c>;
/// the course is stamped with the caller's tenant id.
/// </summary>
public record CreateCourseCommand(
    string Title,
    string? Description,
    string? RegulatoryFramework,
    decimal TotalHours,
    TrainingType TrainingType,
    bool IsActive,
    IReadOnlyList<CreateCourseModuleInput> Modules) : ICommand<CreateCourseResult>;
