using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILessonTemplateRepository"/>.
/// Soft-delete and tenant-isolation are applied automatically via the global
/// query filter registered on <see cref="LessonTemplate"/> in
/// <c>FsbsDbContext</c>.
/// </summary>
internal sealed class LessonTemplateRepository(FsbsDbContext db) : ILessonTemplateRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(LessonTemplate template, CancellationToken ct = default)
    {
        db.LessonTemplates.Add(template);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<LessonTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.LessonTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    /// <inheritdoc/>
    public async Task UpdateAsync(LessonTemplate template, CancellationToken ct = default)
    {
        db.LessonTemplates.Update(template);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public Task<int> CountAttachedLessonsAsync(Guid templateId, CancellationToken ct = default) =>
        db.Lessons.CountAsync(l => l.SourceTemplateId == templateId, ct);
}
