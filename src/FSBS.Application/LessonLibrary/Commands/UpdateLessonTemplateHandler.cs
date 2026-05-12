using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Interfaces;
using FSBS.Shared.LessonLibrary;
using MediatR;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Handler for <see cref="UpdateLessonTemplateCommand"/>. Throws
/// <see cref="LessonTemplateNotFoundException"/> when the id is unknown to the
/// current tenant; relies on EF's <c>xmin</c> concurrency token to surface
/// stale-write attempts as a <c>DbUpdateConcurrencyException</c>.
/// </summary>
public sealed class UpdateLessonTemplateHandler(
    ILessonTemplateRepository templates)
    : IRequestHandler<UpdateLessonTemplateCommand, LessonTemplateDto>
{
    /// <inheritdoc/>
    public async Task<LessonTemplateDto> Handle(
        UpdateLessonTemplateCommand command,
        CancellationToken ct)
    {
        var template = await templates.FindByIdAsync(command.Id, ct)
            ?? throw new LessonTemplateNotFoundException(command.Id);

        template.Title                  = command.Title;
        template.Description            = command.Description;
        template.TrainingType           = command.TrainingType;
        template.DefaultMinDurationMins = command.DefaultMinDurationMins;
        template.RequiresInstructor     = command.RequiresInstructor;
        template.IsMandatoryByDefault   = command.IsMandatoryByDefault;
        template.Category               = command.Category;

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
