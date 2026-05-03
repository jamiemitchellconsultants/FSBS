using FluentValidation;

namespace FSBS.Application.Invitations.Commands;

public sealed class ClaimInvitationCommandValidator : AbstractValidator<ClaimInvitationCommand>
{
    public ClaimInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .Length(64)
            .Matches("^[0-9a-fA-F]{64}$")
            .WithMessage("Token must be a 64-character hex string.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(256);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g. +447911123456).")
            .When(x => x.PhoneNumber is not null);
    }
}
