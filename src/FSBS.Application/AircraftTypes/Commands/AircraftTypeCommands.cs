using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.AircraftTypes.Commands;

public record CreateAircraftTypeCommand(string IcaoCode, string Name) : ICommand<AircraftTypeDto>;

public record UpdateAircraftTypeCommand(Guid AircraftTypeId, string IcaoCode, string Name, bool IsActive) : ICommand<AircraftTypeDto>;

public record DeleteAircraftTypeCommand(Guid AircraftTypeId) : ICommand<Unit>;


