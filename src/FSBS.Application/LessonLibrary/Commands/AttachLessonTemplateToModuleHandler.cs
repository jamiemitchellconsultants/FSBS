using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Shared.LessonLibrary;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Handler for <see cref="AttachLessonTemplateToModuleCommand"/>. Copies the
/// template's fields into a new <see cref="Lesson"/> row (applying overrides
/// when supplied), gating on (1) template is active and not soft-deleted,
/// (2) template's <c>TrainingType</c> matches the parent course's.
/// The DB unique index <c>(module_id, sequence_order)</c> guards against
/// concurrent sequence collisions — these surface as a
/// <c>DbUpdateException</c> which the API layer maps to <c>409 Conflict</c>.
/// </summary>
public sealed class AttachLessonTemplateToModuleHandler(
    ILessonTemplateRepository templates,
    ILessonRepository lessons,
    ILogger<AttachLessonTemplateToModuleHandler> logger)
    : IRequestHandler<AttachLessonTemplateToModuleCommand, LessonDto>
{
    /// <inheritdoc/>
    public async Task<LessonDto> Handle(
        AttachLessonTemplateToModuleCommand command,
        CancellationToken ct)
    {
        var template = await templates.FindByIdAsync(command.LessonTemplateId, ct)
            ?? throw new LessonTemplateNotFoundException(command.LessonTemplateId);

        if (!template.IsActive || template.IsDeleted)
            throw new LessonTemplateInactiveException(template.Id);

        var module = await lessons.FindModuleWithCourseAsync(command.ModuleId, ct)
            ?? throw new ModuleNotFoundException(command.ModuleId);

        if (module.Course.TrainingType != template.TrainingType)
        {
            throw new LessonTemplateTrainingTypeMismatchException(
                template.Id, template.TrainingType, module.Course.TrainingType);
        }

        var lesson = new Lesson
        {
            Id                 = Guid.NewGuid(),
            ModuleId           = module.Id,
            Title              = template.Title,
            SequenceOrder      = command.SequenceOrder,
            MinDurationMins    = command.MinDurationMins    ?? template.DefaultMinDurationMins,
            RequiresInstructor = command.RequiresInstructor ?? template.RequiresInstructor,
            IsMandatory        = command.IsMandatory        ?? template.IsMandatoryByDefault,
            SourceTemplateId   = template.Id,
        };

        await lessons.AddAsync(lesson, ct);

        logger.LogInformation(
            "Lesson {LessonId} attached to module {ModuleId} from template {TemplateId}",
            lesson.Id, module.Id, template.Id);

        return new LessonDto(
            lesson.Id,
            lesson.ModuleId,
            lesson.Title,
            lesson.SequenceOrder,
            lesson.MinDurationMins,
            lesson.RequiresInstructor,
            lesson.IsMandatory,
            lesson.SourceTemplateId);
    }
}
