using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;
using FluentValidation;
using MediatR;

namespace FSBS.Application.Simulators.Commands;

public sealed class CreateSimulatorUnitHandler(ISimulatorRepository simulators)
    : IRequestHandler<CreateSimulatorUnitCommand, SimulatorDetailDto>
{
    public async Task<SimulatorDetailDto> Handle(CreateSimulatorUnitCommand request, CancellationToken ct)
    {
        var unit = new SimulatorUnit
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            FstdLevel = request.FstdLevel.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            Location = request.Location?.Trim(),
            DefaultReconfigMins = request.DefaultReconfigMins,
            IsActive = true,
        };

        await simulators.AddUnitAsync(unit, ct);
        return SimulatorDtoMapper.ToDetail(unit);
    }
}

public sealed class UpdateSimulatorUnitHandler(ISimulatorRepository simulators)
    : IRequestHandler<UpdateSimulatorUnitCommand, SimulatorDetailDto>
{
    public async Task<SimulatorDetailDto> Handle(UpdateSimulatorUnitCommand request, CancellationToken ct)
    {
        var unit = await simulators.FindByIdAsync(request.SimulatorUnitId, ct)
            ?? throw new SimulatorUnitNotFoundException(request.SimulatorUnitId);

        unit.Name = request.Name.Trim();
        unit.FstdLevel = request.FstdLevel.Trim();
        unit.Manufacturer = request.Manufacturer?.Trim();
        unit.Location = request.Location?.Trim();
        unit.DefaultReconfigMins = request.DefaultReconfigMins;
        unit.IsActive = request.IsActive;

        return SimulatorDtoMapper.ToDetail(unit);
    }
}

public sealed class DeleteSimulatorUnitHandler(ISimulatorRepository simulators)
    : IRequestHandler<DeleteSimulatorUnitCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSimulatorUnitCommand request, CancellationToken ct)
    {
        var unit = await simulators.FindByIdAsync(request.SimulatorUnitId, ct)
            ?? throw new SimulatorUnitNotFoundException(request.SimulatorUnitId);

        unit.IsDeleted = true;
        return Unit.Value;
    }
}

public sealed class CreateSimulatorBayHandler(ISimulatorRepository simulators)
    : IRequestHandler<CreateSimulatorBayCommand, SimulatorBayDto>
{
    public async Task<SimulatorBayDto> Handle(CreateSimulatorBayCommand request, CancellationToken ct)
    {
        var unit = await simulators.FindByIdAsync(request.SimulatorUnitId, ct)
            ?? throw new SimulatorUnitNotFoundException(request.SimulatorUnitId);

        var bay = new SimulatorBay
        {
            Id = Guid.NewGuid(),
            SimulatorUnitId = unit.Id,
            BayCode = request.BayCode.Trim(),
            Description = request.Description?.Trim(),
            Status = BayStatus.Operational,
        };

        await simulators.AddBayAsync(bay, ct);
        return SimulatorDtoMapper.ToBay(bay);
    }
}

public sealed class UpdateSimulatorBayHandler(ISimulatorRepository simulators)
    : IRequestHandler<UpdateSimulatorBayCommand, SimulatorBayDto>
{
    public async Task<SimulatorBayDto> Handle(UpdateSimulatorBayCommand request, CancellationToken ct)
    {
        var bay = await simulators.FindBayAsync(request.SimulatorBayId, ct)
            ?? throw new SimulatorBayNotFoundException(request.SimulatorBayId);

        if (bay.SimulatorUnitId != request.SimulatorUnitId)
            throw new SimulatorBayNotFoundException(request.SimulatorBayId);

        if (!Enum.TryParse<BayStatus>(request.Status, out var status))
            throw new ValidationException($"Invalid bay status '{request.Status}'.");

        bay.BayCode = request.BayCode.Trim();
        bay.Description = request.Description?.Trim();
        bay.Status = status;

        return SimulatorDtoMapper.ToBay(bay);
    }
}

public sealed class DeleteSimulatorBayHandler(ISimulatorRepository simulators)
    : IRequestHandler<DeleteSimulatorBayCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSimulatorBayCommand request, CancellationToken ct)
    {
        var bay = await simulators.FindBayAsync(request.SimulatorBayId, ct)
            ?? throw new SimulatorBayNotFoundException(request.SimulatorBayId);

        if (bay.SimulatorUnitId != request.SimulatorUnitId)
            throw new SimulatorBayNotFoundException(request.SimulatorBayId);

        bay.IsDeleted = true;
        return Unit.Value;
    }
}

public sealed class CreateSimulatorConfigurationHandler(
    ISimulatorRepository simulators,
    IAircraftTypeRepository aircraftTypes)
    : IRequestHandler<CreateSimulatorConfigurationCommand, SimulatorConfigurationDto>
{
    public async Task<SimulatorConfigurationDto> Handle(CreateSimulatorConfigurationCommand request, CancellationToken ct)
    {
        var unit = await simulators.FindByIdAsync(request.SimulatorUnitId, ct)
            ?? throw new SimulatorUnitNotFoundException(request.SimulatorUnitId);

        var aircraftType = await aircraftTypes.FindByIdAsync(request.AircraftTypeId, ct)
            ?? throw new AircraftTypeNotFoundException(request.AircraftTypeId);

        if (!Enum.TryParse<ConfigurationMode>(request.ConfigMode, out var configMode))
            throw new ValidationException($"Invalid ConfigMode '{request.ConfigMode}'.");

        var trainingTypes = SimulatorParsing.ParseTrainingTypes(request.SupportedTrainingTypes);

        var config = new SimulatorConfiguration
        {
            Id = Guid.NewGuid(),
            SimulatorUnitId = unit.Id,
            Name = request.Name.Trim(),
            AircraftTypeId = request.AircraftTypeId,
            ConfigMode = configMode,
            SupportedTrainingTypes = trainingTypes,
            MaxCapacityFlightDeck = request.MaxCapacityFlightDeck,
            MaxCapacityCabinCrew = request.MaxCapacityCabinCrew,
            IsActive = true,
            AircraftType = aircraftType,
        };

        await simulators.AddConfigurationAsync(config, ct);
        return SimulatorDtoMapper.ToConfiguration(config);
    }
}

public sealed class UpdateSimulatorConfigurationHandler(
    ISimulatorRepository simulators,
    IAircraftTypeRepository aircraftTypes)
    : IRequestHandler<UpdateSimulatorConfigurationCommand, SimulatorConfigurationDto>
{
    public async Task<SimulatorConfigurationDto> Handle(UpdateSimulatorConfigurationCommand request, CancellationToken ct)
    {
        var config = await simulators.FindConfigurationAsync(request.SimulatorConfigurationId, ct)
            ?? throw new SimulatorConfigurationNotFoundException(request.SimulatorConfigurationId);

        if (config.SimulatorUnitId != request.SimulatorUnitId)
            throw new SimulatorConfigurationNotFoundException(request.SimulatorConfigurationId);

        var aircraftType = await aircraftTypes.FindByIdAsync(request.AircraftTypeId, ct)
            ?? throw new AircraftTypeNotFoundException(request.AircraftTypeId);

        if (!Enum.TryParse<ConfigurationMode>(request.ConfigMode, out var configMode))
            throw new ValidationException($"Invalid ConfigMode '{request.ConfigMode}'.");

        var trainingTypes = SimulatorParsing.ParseTrainingTypes(request.SupportedTrainingTypes);

        config.Name = request.Name.Trim();
        config.AircraftTypeId = request.AircraftTypeId;
        config.AircraftType = aircraftType;
        config.ConfigMode = configMode;
        config.SupportedTrainingTypes = trainingTypes;
        config.MaxCapacityFlightDeck = request.MaxCapacityFlightDeck;
        config.MaxCapacityCabinCrew = request.MaxCapacityCabinCrew;
        config.IsActive = request.IsActive;

        return SimulatorDtoMapper.ToConfiguration(config);
    }
}

public sealed class DeleteSimulatorConfigurationHandler(ISimulatorRepository simulators)
    : IRequestHandler<DeleteSimulatorConfigurationCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSimulatorConfigurationCommand request, CancellationToken ct)
    {
        var config = await simulators.FindConfigurationAsync(request.SimulatorConfigurationId, ct)
            ?? throw new SimulatorConfigurationNotFoundException(request.SimulatorConfigurationId);

        if (config.SimulatorUnitId != request.SimulatorUnitId)
            throw new SimulatorConfigurationNotFoundException(request.SimulatorConfigurationId);

        config.IsDeleted = true;
        return Unit.Value;
    }
}

internal static class SimulatorParsing
{
    public static List<TrainingType> ParseTrainingTypes(IReadOnlyList<string> raw)
    {
        var result = new List<TrainingType>();
        foreach (var s in raw)
        {
            if (!Enum.TryParse<TrainingType>(s, out var t))
                throw new ValidationException($"Invalid TrainingType '{s}'.");
            result.Add(t);
        }

        if (result.Count == 0)
            throw new ValidationException("At least one SupportedTrainingType is required.");

        return result;
    }
}


