using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.Simulators.Queries;

public sealed class GetSimulatorDetailHandler(ISimulatorRepository simulators)
    : IRequestHandler<GetSimulatorDetailQuery, SimulatorDetailDto?>
{
    public async Task<SimulatorDetailDto?> Handle(GetSimulatorDetailQuery request, CancellationToken ct)
    {
        var unit = await simulators.FindByIdAsync(request.SimulatorUnitId, ct);
        return unit is null ? null : SimulatorDtoMapper.ToDetail(unit);
    }
}

