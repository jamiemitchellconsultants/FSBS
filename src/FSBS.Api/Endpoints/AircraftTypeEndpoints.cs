using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;

namespace FSBS.Api.Endpoints;

public static class AircraftTypeEndpoints
{
    public static IEndpointRouteBuilder MapAircraftTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/aircraft-types")
            .WithTags("AircraftTypes")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithName("ListAircraftTypes")
            .WithSummary("Return all aircraft types.")
            .Produces<IReadOnlyList<AircraftTypeDto>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateAircraftType")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AircraftTypeDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateAircraftType")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AircraftTypeDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteAircraftType")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IAircraftTypeRepository repo, CancellationToken ct)
    {
        var items = await repo.ListAllAsync(ct);
        return Results.Ok(items.Select(Map).ToList());
    }

    private static async Task<IResult> CreateAsync(
        CreateAircraftTypeRequest body,
        IAircraftTypeRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.IcaoCode) || string.IsNullOrWhiteSpace(body.Name))
            return Results.Problem(detail: "IcaoCode and Name are required.", statusCode: StatusCodes.Status400BadRequest);

        var existing = (await repo.ListAllAsync(ct))
            .FirstOrDefault(a => string.Equals(a.IcaoCode, body.IcaoCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return Results.Problem(detail: $"Aircraft type '{body.IcaoCode}' already exists.", statusCode: StatusCodes.Status409Conflict);

        var entity = new AircraftType
        {
            Id = Guid.NewGuid(),
            IcaoCode = body.IcaoCode.Trim().ToUpperInvariant(),
            Name = body.Name.Trim(),
            IsActive = true,
        };

        await repo.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/aircraft-types/{entity.Id}", Map(entity));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateAircraftTypeRequest body,
        IAircraftTypeRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db,
        CancellationToken ct)
    {
        var entity = await repo.FindByIdAsync(id, ct);
        if (entity is null) return Results.NotFound();

        entity.IcaoCode = body.IcaoCode.Trim().ToUpperInvariant();
        entity.Name = body.Name.Trim();
        entity.IsActive = body.IsActive;

        await db.SaveChangesAsync(ct);
        return Results.Ok(Map(entity));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IAircraftTypeRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db,
        CancellationToken ct)
    {
        var entity = await repo.FindByIdAsync(id, ct);
        if (entity is null) return Results.NotFound();

        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static AircraftTypeDto Map(AircraftType a) =>
        new(a.Id, a.IcaoCode, a.Name, a.IsActive);
}

public record CreateAircraftTypeRequest(string IcaoCode, string Name);
public record UpdateAircraftTypeRequest(string IcaoCode, string Name, bool IsActive);
