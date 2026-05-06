using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;

namespace FSBS.Api.Endpoints;

public static class SimulatorEndpoints
{
    public static IEndpointRouteBuilder MapSimulatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/simulators")
            .WithTags("Simulators")
            .RequireAuthorization();

        group.MapGet("/{id:guid}/availability", GetAvailabilityAsync)
            .WithName("GetSimulatorAvailability")
            .WithSummary("Return available slots, reconfiguration windows, and maintenance windows for a simulator.")
            .Produces<AvailabilityGridDto>()
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

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
