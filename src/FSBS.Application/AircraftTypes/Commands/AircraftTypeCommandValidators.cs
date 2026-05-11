using FluentValidation;

namespace FSBS.Application.AircraftTypes.Commands;

public sealed class CreateAircraftTypeCommandValidator : AbstractValidator<CreateAircraftTypeCommand>
{
    public CreateAircraftTypeCommandValidator()
    {
        RuleFor(x => x.IcaoCode).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class UpdateAircraftTypeCommandValidator : AbstractValidator<UpdateAircraftTypeCommand>
{
    public UpdateAircraftTypeCommandValidator()
    {
        RuleFor(x => x.AircraftTypeId).NotEmpty();
        RuleFor(x => x.IcaoCode).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class DeleteAircraftTypeCommandValidator : AbstractValidator<DeleteAircraftTypeCommand>
{
    public DeleteAircraftTypeCommandValidator()
    {
        RuleFor(x => x.AircraftTypeId).NotEmpty();
    }
}

