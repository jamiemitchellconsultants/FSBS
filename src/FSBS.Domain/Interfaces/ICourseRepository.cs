using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the <see cref="Course"/> aggregate. Persists the
/// course together with its owned <see cref="Module"/> collection in a single
/// <c>SaveChanges</c> call so the aggregate boundary is honoured.
/// </summary>
public interface ICourseRepository
{
    /// <summary>
    /// Persists a new course (and any child <see cref="Module"/>s attached via
    /// <see cref="Course.Modules"/>) in a single transaction.
    /// </summary>
    Task AddAsync(Course course, CancellationToken ct = default);

    /// <summary>
    /// Loads a course by id with its <see cref="Course.Modules"/> collection
    /// eagerly included. Returns <c>null</c> when the id is not visible to the
    /// current tenant.
    /// </summary>
    Task<Course?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
