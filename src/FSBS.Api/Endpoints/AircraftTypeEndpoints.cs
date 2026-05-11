using FSBS.Application.AircraftTypes.Commands;
using FSBS.Application.AircraftTypes.Queries;
using FSBS.Application.Common.Exceptions;
using FSBS.Shared.AircraftTypes;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for aircraft type reference data management.
/// Routes are under <c>/v1/aircraft-types</c> and require authentication.
/// Write operations require ScheduleAdmin role.
/// </summary>
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
            .WithSummary("Return all available aircraft types (e.g., Boeing 737, Airbus A320).")
            .Produces<IReadOnlyList<AircraftTypeDto>>();

        group.MapPost("/", CreateAsync)
            .WithName("CreateAircraftType")
            .WithSummary("Create a new aircraft type with ICAO code and description (ScheduleAdmin only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AircraftTypeDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateAircraftType")
            .WithSummary("Update an aircraft type's ICAO code and description (ScheduleAdmin only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AircraftTypeDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteAircraftType")
            .WithSummary("Soft-delete an aircraft type (ScheduleAdmin only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender, CancellationToken ct)
    {
        var items = await sender.Send(new ListAircraftTypesQuery(), ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAsync(
        CreateAircraftTypeRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new CreateAircraftTypeCommand(body.IcaoCode, body.Name), ct);
            return Results.Created($"/v1/aircraft-types/{dto.AircraftTypeId}", dto);
        }
        catch (AircraftTypeAlreadyExistsException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateAircraftTypeRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new UpdateAircraftTypeCommand(id, body.IcaoCode, body.Name, body.IsActive), ct);
            return Results.Ok(dto);
        }
        catch (AircraftTypeNotFoundException)
        {
            return Results.NotFound();
        }
        catch (AircraftTypeAlreadyExistsException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteAircraftTypeCommand(id), ct);
            return Results.NoContent();
        }
        catch (AircraftTypeNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
