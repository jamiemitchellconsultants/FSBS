using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core read-side projection for the curriculum lesson library.
/// </summary>
internal sealed class LessonTemplateReadRepository(FsbsDbContext db) : ILessonTemplateReadRepository
{
    /// <inheritdoc/>
    public async Task<PagedResult<LessonTemplateListItemDto>> ListAsync(
        TrainingType? trainingType,
        string? category,
        bool? isActive,
        string? search,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(limit, 1, 100);

        var q = db.LessonTemplates.AsNoTracking().AsQueryable();

        if (trainingType is { } tt) q = q.Where(t => t.TrainingType == tt);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(t => t.Category == category);
        if (isActive is { } active) q = q.Where(t => t.IsActive == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(s));
        }

        // Cursor format: "{titleLowercase}|{id}"
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            var parts = cursor.Split('|', 2);
            if (parts.Length == 2 && Guid.TryParse(parts[1], out var afterId))
            {
                var afterTitle = parts[0];
                q = q.Where(t =>
                    string.Compare(t.Title.ToLower(), afterTitle) > 0
                    || (t.Title.ToLower() == afterTitle && t.Id.CompareTo(afterId) > 0));
            }
        }

        var rows = await q
            .OrderBy(t => t.Title.ToLower())
            .ThenBy(t => t.Id)
            .Take(pageSize + 1)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.TrainingType,
                t.Category,
                t.IsActive,
                UsageCount = db.Lessons.Count(l => l.SourceTemplateId == t.Id),
            })
            .ToListAsync(ct);

        string? nextCursor = null;
        if (rows.Count > pageSize)
        {
            var last = rows[pageSize - 1];
            nextCursor = $"{last.Title.ToLower()}|{last.Id}";
            rows.RemoveAt(pageSize);
        }

        var items = rows
            .Select(r => new LessonTemplateListItemDto(
                r.Id, r.Title, r.TrainingType, r.Category, r.IsActive, r.UsageCount))
            .ToList();

        return new PagedResult<LessonTemplateListItemDto>(items, nextCursor);
    }

    /// <inheritdoc/>
    public async Task<LessonTemplateDto?> GetAsync(Guid templateId, CancellationToken ct = default)
    {
        return await db.LessonTemplates.AsNoTracking()
            .Where(t => t.Id == templateId)
            .Select(t => new LessonTemplateDto(
                t.Id,
                t.Title,
                t.Description,
                t.TrainingType,
                t.DefaultMinDurationMins,
                t.RequiresInstructor,
                t.IsMandatoryByDefault,
                t.Category,
                t.IsActive,
                db.Lessons.Count(l => l.SourceTemplateId == t.Id)))
            .FirstOrDefaultAsync(ct);
    }
}
