using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.AircraftTypes.Queries;

public sealed class ListAircraftTypesHandler(IAircraftTypeRepository repo)
    : IRequestHandler<ListAircraftTypesQuery, IReadOnlyList<AircraftTypeDto>>
{
    public async Task<IReadOnlyList<AircraftTypeDto>> Handle(ListAircraftTypesQuery request, CancellationToken ct)
    {
        var items = await repo.ListAllAsync(ct);
        return items
            .Select(a => new AircraftTypeDto(a.Id, a.IcaoCode, a.Name, a.IsActive))
            .ToList();
    }
}

