using FluentValidation;

namespace FSBS.Application.Courses.Commands;

/// <summary>
/// FluentValidation rules for <see cref="CreateCourseCommand"/>. Mirrors the
/// DB-level constraints on <c>courses</c> and <c>modules</c> so invalid input
/// is rejected before reaching the handler.
/// </summary>
public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    /// <summary>Module-collection cap to keep payloads sane.</summary>
    private const int MaxModuleCount = 50;

    /// <summary>Configures the FluentValidation ruleset.</summary>
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.RegulatoryFramework)
            .MaximumLength(100)
            .When(x => x.RegulatoryFramework is not null);

        RuleFor(x => x.TotalHours)
            .GreaterThan(0m)
            .LessThan(10000m)
            .PrecisionScale(6, 1, ignoreTrailingZeros: true);

        RuleFor(x => x.TrainingType).IsInEnum();

        RuleFor(x => x.Modules)
            .NotNull()
            .Must(m => m.Count <= MaxModuleCount)
            .WithMessage($"At most {MaxModuleCount} modules can be created in one request.");

        RuleForEach(x => x.Modules).ChildRules(m =>
        {
            m.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            m.RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
            m.RuleFor(x => x.SequenceOrder).GreaterThanOrEqualTo(1);
        });

        // Collection-level: sequence numbers must be unique within the request.
        RuleFor(x => x.Modules)
            .Must(modules => modules.Select(m => m.SequenceOrder).Distinct().Count() == modules.Count)
            .WithMessage("Module sequence numbers must be unique within the request.")
            .When(x => x.Modules is { Count: > 0 });
    }
}
