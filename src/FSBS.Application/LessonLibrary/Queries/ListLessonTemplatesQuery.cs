using FSBS.Domain.Enums;
using FSBS.Shared.Common;
using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Queries;

/// <summary>
/// Cursor-paginated list of lesson templates for the calling tenant.
/// All filters are AND-combined. When <paramref name="IsActive"/> is null
/// the result returns both active and retired templates.
/// </summary>
public record ListLessonTemplatesQuery(
    TrainingType? TrainingType,
    string? Category,
    bool? IsActive,
    string? Search,
    string? Cursor,
    int Limit) : IRequest<PagedResult<LessonTemplateListItemDto>>;
