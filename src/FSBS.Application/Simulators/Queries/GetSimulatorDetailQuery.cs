using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.Simulators.Queries;

public record GetSimulatorDetailQuery(Guid SimulatorUnitId) : IRequest<SimulatorDetailDto?>;

