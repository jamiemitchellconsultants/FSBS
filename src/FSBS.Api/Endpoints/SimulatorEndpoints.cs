using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for querying simulators and availability.
/// Routes are under <c>/v1/simulators</c> and require authentication.
/// Availability responses are served from the Redis availability cache when available (60-second TTL).
/// </summary>
public static class SimulatorEndpoints
{
    public static IEndpointRouteBuilder MapSimulatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/simulators")
            .WithTags("Simulators")
            .RequireAuthorization();

        group.MapGet("/", ListSimulatorsAsync)
            .WithName("ListSimulators")
            .WithSummary("Return all active simulator units with their bays and configurations.")
            .Produces<SimulatorListResponse>();

        group.MapGet("/{id:guid}", GetSimulatorDetailAsync)
            .WithName("GetSimulatorDetail")
            .WithSummary("Return a single simulator unit with its bays and configurations.")
            .Produces<SimulatorDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/availability", GetAvailabilityAsync)
            .WithName("GetSimulatorAvailability")
            .WithSummary("Return available slots, reconfiguration windows, and maintenance windows for a simulator.")
            .Produces<AvailabilityGridDto>()
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> ListSimulatorsAsync(
        ISimulatorRepository simulatorRepository,
        CancellationToken ct)
    {
        // ISimulatorRepository is write-side; for the list we query via EF directly.
        // We reuse FindByIdAsync pattern — but since there's no ListAll, we expose
        // a lightweight projection via the repository's DbContext through a query service.
        // For now, delegate to the detail endpoint pattern using a direct EF query.
        // This is intentionally simple — a dedicated query/Dapper read can replace it later.
        var items = await simulatorRepository.ListAllAsync(ct);
        return Results.Ok(new SimulatorListResponse(items.Select(MapToDetail).ToList()));
    }

    private static async Task<IResult> GetSimulatorDetailAsync(
        Guid id,
        ISimulatorRepository simulatorRepository,
        CancellationToken ct)
    {
        var unit = await simulatorRepository.FindByIdAsync(id, ct);
        if (unit is null)
            return Results.NotFound();

        return Results.Ok(MapToDetail(unit));
    }

    private static SimulatorDetailDto MapToDetail(Domain.Entities.SimulatorUnit unit) =>
        new(
            UnitId: unit.Id,
            Name: unit.Name,
            FstdLevel: unit.FstdLevel,
            IsActive: unit.IsActive,
            Bays: unit.Bays
                .Where(b => !b.IsDeleted && b.Status == BayStatus.Operational)
                .Select(b => new SimulatorBayDto(b.Id, b.BayCode, b.Status.ToString()))
                .ToList(),
            Configurations: unit.Configurations
                .Where(c => !c.IsDeleted && c.IsActive)
                .Select(c => new SimulatorConfigurationDto(
                    c.Id,
                    c.Name,
                    c.AircraftType,
                    c.ConfigMode.ToString(),
                    c.SupportedTrainingTypes.Select(t => t.ToString()).ToList(),
                    c.MaxCapacityFlightDeck,
                    c.MaxCapacityCabinCrew,
                    c.IsActive))
                .ToList());

    private static async Task<IResult> GetAvailabilityAsync(
        Guid id,
        DateTimeOffset from,
        DateTimeOffset to,
        IAvailabilityCache cache,
        IAvailabilityReadService readService,
        CancellationToken ct)
    {
        if (from >= to)
            return Results.Problem(
                detail: "'from' must be earlier than 'to'.",
                statusCode: StatusCodes.Status400BadRequest);

        // Serve from Redis cache when available (60-second TTL).
        var cached = await cache.GetAsync(id, from, to, ct);
        if (cached is not null)
            return Results.Ok(cached);

        // Cache miss — query the DB via Dapper and populate the cache.
        var grid = await readService.GetAvailabilityAsync(id, from, to, ct);
        await cache.SetAsync(id, from, to, grid, ct);

        return Results.Ok(grid);
    }
}

public record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);
