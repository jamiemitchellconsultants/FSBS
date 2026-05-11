using FluentValidation;

namespace FSBS.Application.UserProfiles.Commands;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.Profile.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Profile.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Profile.PhoneNumber).MaximumLength(30).When(x => x.Profile.PhoneNumber is not null);
        RuleFor(x => x.Profile.LicenceNumber).MaximumLength(50).When(x => x.Profile.LicenceNumber is not null);
    }
}
