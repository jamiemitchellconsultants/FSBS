using FluentValidation;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>Validation rules for <see cref="AttachLessonTemplateToModuleCommand"/>.</summary>
public sealed class AttachLessonTemplateToModuleCommandValidator
    : AbstractValidator<AttachLessonTemplateToModuleCommand>
{
    /// <summary>Configures the FluentValidation ruleset.</summary>
    public AttachLessonTemplateToModuleCommandValidator()
    {
        RuleFor(x => x.LessonTemplateId).NotEmpty();
        RuleFor(x => x.ModuleId).NotEmpty();
        RuleFor(x => x.SequenceOrder).GreaterThanOrEqualTo(1);

        RuleFor(x => x.MinDurationMins!.Value)
            .InclusiveBetween(1, 1440)
            .When(x => x.MinDurationMins.HasValue);
    }
}
