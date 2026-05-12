using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the curriculum <see cref="LessonTemplate"/>
/// aggregate. Read-side projections used by list/grid endpoints live in
/// <c>ILessonTemplateReadRepository</c>.
/// </summary>
public interface ILessonTemplateRepository
{
    /// <summary>Persists a new template. Audit columns are stamped by the interceptor.</summary>
    Task AddAsync(LessonTemplate template, CancellationToken ct = default);

    /// <summary>Loads a template by id, including the current tenant's soft-delete filter.</summary>
    Task<LessonTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Persists changes to an already-tracked template. Concurrency is enforced
    /// by the <c>xmin</c> token configured in the entity's EF Fluent API config.
    /// </summary>
    Task UpdateAsync(LessonTemplate template, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of <see cref="Lesson"/> rows currently referencing this
    /// template via <see cref="Lesson.SourceTemplateId"/>. Used for soft-delete
    /// confirmation prompts.
    /// </summary>
    Task<int> CountAttachedLessonsAsync(Guid templateId, CancellationToken ct = default);
}
