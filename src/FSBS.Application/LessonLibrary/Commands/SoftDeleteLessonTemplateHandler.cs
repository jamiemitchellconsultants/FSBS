using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>Handler for <see cref="SoftDeleteLessonTemplateCommand"/>.</summary>
public sealed class SoftDeleteLessonTemplateHandler(
    ILessonTemplateRepository templates)
    : IRequestHandler<SoftDeleteLessonTemplateCommand, Unit>
{
    /// <inheritdoc/>
    public async Task<Unit> Handle(
        SoftDeleteLessonTemplateCommand command,
        CancellationToken ct)
    {
        var template = await templates.FindByIdAsync(command.Id, ct)
            ?? throw new LessonTemplateNotFoundException(command.Id);

        template.IsDeleted = true;
        await templates.UpdateAsync(template, ct);
        return Unit.Value;
    }
}
