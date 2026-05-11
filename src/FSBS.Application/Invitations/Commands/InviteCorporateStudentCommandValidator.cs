using FluentValidation;

namespace FSBS.Application.Invitations.Commands;

public sealed class InviteCorporateStudentCommandValidator
    : AbstractValidator<InviteCorporateStudentCommand>
{
    public InviteCorporateStudentCommandValidator()
    {
        RuleFor(x => x.InviteeEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.PersonalNote)
            .MaximumLength(1000)
            .When(x => x.PersonalNote is not null);
    }
}
