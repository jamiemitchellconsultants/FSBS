using FluentValidation;

namespace FSBS.Application.UserProfiles.Commands;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.Profile.FirstName).NotEmpty();
        RuleFor(x => x.Profile.LastName).NotEmpty();
    }
}

