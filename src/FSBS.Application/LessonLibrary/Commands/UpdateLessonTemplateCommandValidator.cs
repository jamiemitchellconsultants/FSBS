using FluentValidation;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>Validation rules for <see cref="UpdateLessonTemplateCommand"/>.</summary>
public sealed class UpdateLessonTemplateCommandValidator
    : AbstractValidator<UpdateLessonTemplateCommand>
{
    /// <summary>Configures the FluentValidation ruleset.</summary>
    public UpdateLessonTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        RuleFor(x => x.DefaultMinDurationMins).InclusiveBetween(1, 1440);
        RuleFor(x => x.TrainingType).IsInEnum();
    }
}
