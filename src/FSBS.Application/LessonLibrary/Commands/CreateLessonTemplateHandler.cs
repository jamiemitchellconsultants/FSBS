using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Shared.LessonLibrary;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Handler for <see cref="CreateLessonTemplateCommand"/>. Stamps the new
/// template with the caller's tenant id, persists it, and returns the projection
/// the caller needs for the UI (including a usage count of zero).
/// </summary>
public sealed class CreateLessonTemplateHandler(
    ICurrentUser currentUser,
    ILessonTemplateRepository templates,
    ILogger<CreateLessonTemplateHandler> logger)
    : IRequestHandler<CreateLessonTemplateCommand, LessonTemplateDto>
{
    /// <inheritdoc/>
    public async Task<LessonTemplateDto> Handle(
        CreateLessonTemplateCommand command,
        CancellationToken ct)
    {
        var template = new LessonTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = currentUser.TenantId,
            Title = command.Title,
            Description = command.Description,
            TrainingType = command.TrainingType,
            DefaultMinDurationMins = command.DefaultMinDurationMins,
            RequiresInstructor = command.RequiresInstructor,
            IsMandatoryByDefault = command.IsMandatoryByDefault,
            Category = command.Category,
            IsActive = true,
        };

        await templates.AddAsync(template, ct);

        logger.LogInformation(
            "Lesson template {TemplateId} created by {UserId} in tenant {TenantId}",
            template.Id, currentUser.UserId, currentUser.TenantId);

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
            UsageCount: 0);
    }
}
