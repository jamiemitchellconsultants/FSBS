using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Queries;

/// <summary>Handler for <see cref="ListLessonTemplatesQuery"/>.</summary>
public sealed class ListLessonTemplatesHandler(ILessonTemplateReadRepository repo)
    : IRequestHandler<ListLessonTemplatesQuery, PagedResult<LessonTemplateListItemDto>>
{
    /// <inheritdoc/>
    public Task<PagedResult<LessonTemplateListItemDto>> Handle(
        ListLessonTemplatesQuery query, CancellationToken ct) =>
        repo.ListAsync(
            query.TrainingType,
            query.Category,
            query.IsActive,
            query.Search,
            query.Cursor,
            query.Limit,
            ct);
}
