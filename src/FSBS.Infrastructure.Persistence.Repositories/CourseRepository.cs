using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICourseRepository"/>. Tenant isolation
/// and soft-delete are applied automatically via the global query filter on
/// <see cref="Course"/>.
/// </summary>
internal sealed class CourseRepository(FsbsDbContext db) : ICourseRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Course course, CancellationToken ct = default)
    {
        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<Course?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Courses
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
