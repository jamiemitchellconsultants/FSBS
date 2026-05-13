namespace FSBS.Shared.Courses;

/// <summary>
/// Module entry embedded in <see cref="CreateCourseRequest.Modules"/>. The
/// server creates one <c>Module</c> row per entry, in the order supplied;
/// <see cref="SequenceOrder"/> values must be unique within the request.
/// </summary>
public record CreateCourseModuleRequest(
    string Title,
    int SequenceOrder,
    string? Description);
