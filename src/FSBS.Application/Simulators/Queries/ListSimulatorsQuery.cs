using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.Simulators.Queries;

public record ListSimulatorsQuery : IRequest<IReadOnlyList<SimulatorDetailDto>>;

