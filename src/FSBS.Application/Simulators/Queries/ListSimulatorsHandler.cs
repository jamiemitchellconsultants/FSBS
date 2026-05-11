using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.Simulators.Queries;

public sealed class ListSimulatorsHandler(ISimulatorRepository simulators)
    : IRequestHandler<ListSimulatorsQuery, IReadOnlyList<SimulatorDetailDto>>
{
    public async Task<IReadOnlyList<SimulatorDetailDto>> Handle(ListSimulatorsQuery request, CancellationToken ct)
    {
        var items = await simulators.ListAllAsync(ct);
        return items.Select(SimulatorDtoMapper.ToDetail).ToList();
    }
}

