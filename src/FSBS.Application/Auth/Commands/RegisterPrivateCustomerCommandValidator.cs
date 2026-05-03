using FluentValidation;

namespace FSBS.Application.Auth.Commands;

public sealed class RegisterPrivateCustomerCommandValidator
    : AbstractValidator<RegisterPrivateCustomerCommand>
{
    public RegisterPrivateCustomerCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        // Cognito default password policy: min 8 chars, upper, lower, digit, symbol.
        // Additional constraints are enforced by the Cognito pool policy itself;
        // we keep the validator lightweight to avoid duplicating that logic here.
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
            .MaximumLength(30)
            // E.164 format required by Cognito (e.g. +447911123456)
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g. +447911123456).")
            .When(x => x.PhoneNumber is not null);
    }
}
