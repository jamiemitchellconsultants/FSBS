using FluentValidation;

namespace FSBS.Application.Auth.Commands;

public sealed class ResendConfirmationCodeCommandValidator
    : AbstractValidator<ResendConfirmationCodeCommand>
{
    public ResendConfirmationCodeCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
