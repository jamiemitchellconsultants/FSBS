using FSBS.Domain.Enums;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;

namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

/// <summary>
/// Read-side projection repository for the curriculum lesson library.
/// Returns DTO shapes directly to query handlers; bypasses the entity tracker.
/// </summary>
public interface ILessonTemplateReadRepository
{
    /// <summary>
    /// Cursor-paginated list of templates for the current tenant. Filters
    /// compose with AND semantics. Cursor is an opaque string returned in
    /// <see cref="PagedResult{T}.NextCursor"/>.
    /// </summary>
    Task<PagedResult<LessonTemplateListItemDto>> ListAsync(
        TrainingType? trainingType,
        string? category,
        bool? isActive,
        string? search,
        string? cursor,
        int limit,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail DTO for a single template, including the live
    /// usage count of attached <c>Lessons</c>. Null if the template does not
    /// exist (or is filtered out by tenant/soft-delete).
    /// </summary>
    Task<LessonTemplateDto?> GetAsync(Guid templateId, CancellationToken ct = default);
}
