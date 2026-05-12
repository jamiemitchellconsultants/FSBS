using FluentValidation;

namespace FSBS.Application.LessonLibrary.Commands;

/// <summary>
/// Validation rules for <see cref="CreateLessonTemplateCommand"/>.
/// Mirrors the DB-level constraints on <c>lesson_templates</c>.
/// </summary>
public sealed class CreateLessonTemplateCommandValidator
    : AbstractValidator<CreateLessonTemplateCommand>
{
    /// <summary>Configures the FluentValidation ruleset.</summary>
    public CreateLessonTemplateCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => x.Category is not null);

        RuleFor(x => x.DefaultMinDurationMins)
            .InclusiveBetween(1, 1440);

        RuleFor(x => x.TrainingType)
            .IsInEnum();
    }
}
