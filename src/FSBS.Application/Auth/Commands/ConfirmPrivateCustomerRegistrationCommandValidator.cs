using FluentValidation;

namespace FSBS.Application.Auth.Commands;

public sealed class ConfirmPrivateCustomerRegistrationCommandValidator
    : AbstractValidator<ConfirmPrivateCustomerRegistrationCommand>
{
    public ConfirmPrivateCustomerRegistrationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.ConfirmationCode)
            .NotEmpty()
            .Matches(@"^\d{6}$")
            .WithMessage("Confirmation code must be exactly 6 digits.");
    }
}
