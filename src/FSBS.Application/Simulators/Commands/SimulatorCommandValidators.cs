using FluentValidation;

namespace FSBS.Application.Simulators.Commands;

public sealed class CreateSimulatorUnitCommandValidator : AbstractValidator<CreateSimulatorUnitCommand>
{
    public CreateSimulatorUnitCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.FstdLevel).NotEmpty();
        RuleFor(x => x.DefaultReconfigMins).GreaterThan(0);
    }
}

public sealed class UpdateSimulatorUnitCommandValidator : AbstractValidator<UpdateSimulatorUnitCommand>
{
    public UpdateSimulatorUnitCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.FstdLevel).NotEmpty();
        RuleFor(x => x.DefaultReconfigMins).GreaterThan(0);
    }
}

public sealed class DeleteSimulatorUnitCommandValidator : AbstractValidator<DeleteSimulatorUnitCommand>
{
    public DeleteSimulatorUnitCommandValidator() => RuleFor(x => x.SimulatorUnitId).NotEmpty();
}

public sealed class CreateSimulatorBayCommandValidator : AbstractValidator<CreateSimulatorBayCommand>
{
    public CreateSimulatorBayCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.BayCode).NotEmpty();
    }
}

public sealed class UpdateSimulatorBayCommandValidator : AbstractValidator<UpdateSimulatorBayCommand>
{
    public UpdateSimulatorBayCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.SimulatorBayId).NotEmpty();
        RuleFor(x => x.BayCode).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public sealed class DeleteSimulatorBayCommandValidator : AbstractValidator<DeleteSimulatorBayCommand>
{
    public DeleteSimulatorBayCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.SimulatorBayId).NotEmpty();
    }
}

public sealed class CreateSimulatorConfigurationCommandValidator : AbstractValidator<CreateSimulatorConfigurationCommand>
{
    public CreateSimulatorConfigurationCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.AircraftTypeId).NotEmpty();
        RuleFor(x => x.ConfigMode).NotEmpty();
        RuleFor(x => x.SupportedTrainingTypes).NotEmpty();
    }
}

public sealed class UpdateSimulatorConfigurationCommandValidator : AbstractValidator<UpdateSimulatorConfigurationCommand>
{
    public UpdateSimulatorConfigurationCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.SimulatorConfigurationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.AircraftTypeId).NotEmpty();
        RuleFor(x => x.ConfigMode).NotEmpty();
        RuleFor(x => x.SupportedTrainingTypes).NotEmpty();
    }
}

public sealed class DeleteSimulatorConfigurationCommandValidator : AbstractValidator<DeleteSimulatorConfigurationCommand>
{
    public DeleteSimulatorConfigurationCommandValidator()
    {
        RuleFor(x => x.SimulatorUnitId).NotEmpty();
        RuleFor(x => x.SimulatorConfigurationId).NotEmpty();
    }
}

