using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Queries;

/// <summary>Handler for <see cref="GetLessonTemplateByIdQuery"/>.</summary>
public sealed class GetLessonTemplateByIdHandler(ILessonTemplateReadRepository repo)
    : IRequestHandler<GetLessonTemplateByIdQuery, LessonTemplateDto?>
{
    /// <inheritdoc/>
    public Task<LessonTemplateDto?> Handle(GetLessonTemplateByIdQuery query, CancellationToken ct) =>
        repo.GetAsync(query.Id, ct);
}
