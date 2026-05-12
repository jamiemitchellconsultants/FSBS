using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Queries;

/// <summary>
/// Full-detail lookup of a single template, including the current usage count
/// of attached <c>Lessons</c>. Returns <c>null</c> when the id is not visible
/// to the calling tenant.
/// </summary>
public record GetLessonTemplateByIdQuery(Guid Id) : IRequest<LessonTemplateDto?>;
