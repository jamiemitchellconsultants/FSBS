using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for individual <see cref="Lesson"/> rows.
/// Currently used by the LessonLibrary "attach from template" flow; other
/// lesson authoring paths will extend this contract.
/// </summary>
public interface ILessonRepository
{
    /// <summary>
    /// Persists a new lesson attached to a module. Audit columns are stamped
    /// by the interceptor; the unique <c>(module_id, sequence_order)</c>
    /// index will surface duplicates as <c>DbUpdateException</c>.
    /// </summary>
    Task AddAsync(Lesson lesson, CancellationToken ct = default);

    /// <summary>
    /// Loads a lesson by id together with its parent <see cref="Module"/> and
    /// <see cref="Course"/> — used by validation flows that need the course's
    /// <c>TrainingType</c> to gate operations.
    /// </summary>
    Task<Lesson?> FindByIdWithCourseAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Loads a <see cref="Module"/> by id together with its parent <see cref="Course"/>.
    /// Used by attach flows that must enforce <c>course.TrainingType</c> gates.
    /// </summary>
    Task<Module?> FindModuleWithCourseAsync(Guid moduleId, CancellationToken ct = default);
}
