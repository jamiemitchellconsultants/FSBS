using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.AircraftTypes.Queries;

public record ListAircraftTypesQuery : IRequest<IReadOnlyList<AircraftTypeDto>>;

