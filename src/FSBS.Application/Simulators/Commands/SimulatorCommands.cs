using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.Simulators.Commands;

public record CreateSimulatorUnitCommand(
    string Name,
    string FstdLevel,
    string? Manufacturer,
    string? Location,
    int DefaultReconfigMins) : ICommand<SimulatorDetailDto>;

public record UpdateSimulatorUnitCommand(
    Guid SimulatorUnitId,
    string Name,
    string FstdLevel,
    string? Manufacturer,
    string? Location,
    int DefaultReconfigMins,
    bool IsActive) : ICommand<SimulatorDetailDto>;

public record DeleteSimulatorUnitCommand(Guid SimulatorUnitId) : ICommand<Unit>;

public record CreateSimulatorBayCommand(
    Guid SimulatorUnitId,
    string BayCode,
    string? Description) : ICommand<SimulatorBayDto>;

public record UpdateSimulatorBayCommand(
    Guid SimulatorUnitId,
    Guid SimulatorBayId,
    string BayCode,
    string? Description,
    string Status) : ICommand<SimulatorBayDto>;

public record DeleteSimulatorBayCommand(
    Guid SimulatorUnitId,
    Guid SimulatorBayId) : ICommand<Unit>;

public record CreateSimulatorConfigurationCommand(
    Guid SimulatorUnitId,
    string Name,
    Guid AircraftTypeId,
    string ConfigMode,
    IReadOnlyList<string> SupportedTrainingTypes,
    int MaxCapacityFlightDeck,
    int MaxCapacityCabinCrew) : ICommand<SimulatorConfigurationDto>;

public record UpdateSimulatorConfigurationCommand(
    Guid SimulatorUnitId,
    Guid SimulatorConfigurationId,
    string Name,
    Guid AircraftTypeId,
    string ConfigMode,
    IReadOnlyList<string> SupportedTrainingTypes,
    int MaxCapacityFlightDeck,
    int MaxCapacityCabinCrew,
    bool IsActive) : ICommand<SimulatorConfigurationDto>;

public record DeleteSimulatorConfigurationCommand(
    Guid SimulatorUnitId,
    Guid SimulatorConfigurationId) : ICommand<Unit>;

