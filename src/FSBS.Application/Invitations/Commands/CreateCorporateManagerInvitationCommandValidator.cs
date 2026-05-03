using FluentValidation;

namespace FSBS.Application.Invitations.Commands;

public sealed class CreateCorporateManagerInvitationCommandValidator
    : AbstractValidator<CreateCorporateManagerInvitationCommand>
{
    public CreateCorporateManagerInvitationCommandValidator()
    {
        RuleFor(x => x.InviteeEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.OrgId)
            .NotEmpty();
    }
}
