using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILessonRepository"/>.
/// </summary>
internal sealed class LessonRepository(FsbsDbContext db) : ILessonRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
    {
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<Lesson?> FindByIdWithCourseAsync(Guid id, CancellationToken ct = default) =>
        db.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    /// <inheritdoc/>
    public Task<Module?> FindModuleWithCourseAsync(Guid moduleId, CancellationToken ct = default) =>
        db.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId, ct);
}
