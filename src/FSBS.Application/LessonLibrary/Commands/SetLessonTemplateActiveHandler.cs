using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Interfaces;
using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>Handler for <see cref="SetLessonTemplateActiveCommand"/>.</summary>
public sealed class SetLessonTemplateActiveHandler(
    ILessonTemplateRepository templates)
    : IRequestHandler<SetLessonTemplateActiveCommand, LessonTemplateDto>
{
    /// <inheritdoc/>
    public async Task<LessonTemplateDto> Handle(
        SetLessonTemplateActiveCommand command,
        CancellationToken ct)
    {
        var template = await templates.FindByIdAsync(command.Id, ct)
            ?? throw new LessonTemplateNotFoundException(command.Id);

        template.IsActive = command.IsActive;
        await templates.UpdateAsync(template, ct);

        var usageCount = await templates.CountAttachedLessonsAsync(template.Id, ct);

        return new LessonTemplateDto(
            template.Id,
            template.Title,
            template.Description,
            template.TrainingType,
            template.DefaultMinDurationMins,
            template.RequiresInstructor,
            template.IsMandatoryByDefault,
            template.Category,
            template.IsActive,
            usageCount);
    }
}
