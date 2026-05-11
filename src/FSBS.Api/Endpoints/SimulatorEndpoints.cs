using FSBS.Application.Common.Interfaces;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Simulators.Commands;
using FSBS.Application.Simulators.Queries;
using FSBS.Shared.Simulators;
using FluentValidation;
using MediatR;

namespace FSBS.Api.Endpoints;

public static class SimulatorEndpoints
{
    public static IEndpointRouteBuilder MapSimulatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/simulators")
            .WithTags("Simulators")
            .RequireAuthorization();

        // ── Read ──────────────────────────────────────────────────────────────

        group.MapGet("/", ListSimulatorsAsync)
            .WithName("ListSimulators")
            .WithSummary("Return all simulator units with their bays and configurations.")
            .Produces<SimulatorListResponse>();

        group.MapGet("/{id:guid}", GetSimulatorDetailAsync)
            .WithName("GetSimulatorDetail")
            .WithSummary("Return a single simulator unit with its bays and configurations.")
            .Produces<SimulatorDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/availability", GetAvailabilityAsync)
            .WithName("GetSimulatorAvailability")
            .WithSummary("Return available slots and reconfiguration windows for a simulator.")
            .Produces<AvailabilityGridDto>()
            .Produces(StatusCodes.Status400BadRequest);

        // ── SimulatorUnit write ───────────────────────────────────────────────

        group.MapPost("/", CreateUnitAsync)
            .WithName("CreateSimulatorUnit")
            .WithSummary("Create a new simulator unit.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorDetailDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateUnitAsync)
            .WithName("UpdateSimulatorUnit")
            .WithSummary("Update a simulator unit's details.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteUnitAsync)
            .WithName("DeleteSimulatorUnit")
            .WithSummary("Soft-delete a simulator unit.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── SimulatorBay write ────────────────────────────────────────────────

        group.MapPost("/{id:guid}/bays", CreateBayAsync)
            .WithName("CreateSimulatorBay")
            .WithSummary("Add a bay to a simulator unit.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorBayDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/bays/{bayId:guid}", UpdateBayAsync)
            .WithName("UpdateSimulatorBay")
            .WithSummary("Update a simulator bay.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorBayDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/bays/{bayId:guid}", DeleteBayAsync)
            .WithName("DeleteSimulatorBay")
            .WithSummary("Soft-delete a simulator bay.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── SimulatorConfiguration write ──────────────────────────────────────

        group.MapPost("/{id:guid}/configurations", CreateConfigurationAsync)
            .WithName("CreateSimulatorConfiguration")
            .WithSummary("Add a configuration to a simulator unit.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorConfigurationDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/configurations/{configId:guid}", UpdateConfigurationAsync)
            .WithName("UpdateSimulatorConfiguration")
            .WithSummary("Update a simulator configuration.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<SimulatorConfigurationDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/configurations/{configId:guid}", DeleteConfigurationAsync)
            .WithName("DeleteSimulatorConfiguration")
            .WithSummary("Soft-delete a simulator configuration.")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Read handlers ─────────────────────────────────────────────────────────

    private static async Task<IResult> ListSimulatorsAsync(
        ISender sender, CancellationToken ct)
    {
        var items = await sender.Send(new ListSimulatorsQuery(), ct);
        return Results.Ok(new SimulatorListResponse(items));
    }

    private static async Task<IResult> GetSimulatorDetailAsync(
        Guid id, ISender sender, CancellationToken ct)
    {
        var unit = await sender.Send(new GetSimulatorDetailQuery(id), ct);
        return unit is null ? Results.NotFound() : Results.Ok(unit);
    }

    private static async Task<IResult> GetAvailabilityAsync(
        Guid id, DateTimeOffset from, DateTimeOffset to,
        IAvailabilityCache cache, IAvailabilityReadService readService, CancellationToken ct)
    {
        if (from >= to)
            return Results.Problem(detail: "'from' must be earlier than 'to'.", statusCode: StatusCodes.Status400BadRequest);

        var cached = await cache.GetAsync(id, from, to, ct);
        if (cached is not null) return Results.Ok(cached);

        var grid = await readService.GetAvailabilityAsync(id, from, to, ct);
        await cache.SetAsync(id, from, to, grid, ct);
        return Results.Ok(grid);
    }

    // ── SimulatorUnit write handlers ──────────────────────────────────────────

    private static async Task<IResult> CreateUnitAsync(
        CreateSimulatorUnitRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new CreateSimulatorUnitCommand(
                body.Name,
                body.FstdLevel,
                body.Manufacturer,
                body.Location,
                body.DefaultReconfigMins), ct);

            return Results.Created($"/v1/simulators/{dto.UnitId}", dto);
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateUnitAsync(
        Guid id,
        UpdateSimulatorUnitRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new UpdateSimulatorUnitCommand(
                id,
                body.Name,
                body.FstdLevel,
                body.Manufacturer,
                body.Location,
                body.DefaultReconfigMins,
                body.IsActive), ct);

            return Results.Ok(dto);
        }
        catch (SimulatorUnitNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteUnitAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteSimulatorUnitCommand(id), ct);
            return Results.NoContent();
        }
        catch (SimulatorUnitNotFoundException)
        {
            return Results.NotFound();
        }
    }

    // ── SimulatorBay write handlers ───────────────────────────────────────────

    private static async Task<IResult> CreateBayAsync(
        Guid id,
        CreateSimulatorBayRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new CreateSimulatorBayCommand(id, body.BayCode, body.Description), ct);
            return Results.Created($"/v1/simulators/{id}/bays/{dto.BayId}", dto);
        }
        catch (SimulatorUnitNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateBayAsync(
        Guid id,
        Guid bayId,
        UpdateSimulatorBayRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new UpdateSimulatorBayCommand(id, bayId, body.BayCode, body.Description, body.Status), ct);
            return Results.Ok(dto);
        }
        catch (SimulatorBayNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteBayAsync(
        Guid id,
        Guid bayId,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteSimulatorBayCommand(id, bayId), ct);
            return Results.NoContent();
        }
        catch (SimulatorBayNotFoundException)
        {
            return Results.NotFound();
        }
    }

    // ── SimulatorConfiguration write handlers ─────────────────────────────────

    private static async Task<IResult> CreateConfigurationAsync(
        Guid id,
        CreateSimulatorConfigurationRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new CreateSimulatorConfigurationCommand(
                id,
                body.Name,
                body.AircraftTypeId,
                body.ConfigMode,
                body.SupportedTrainingTypes,
                body.MaxCapacityFlightDeck,
                body.MaxCapacityCabinCrew), ct);

            return Results.Created($"/v1/simulators/{id}/configurations/{dto.ConfigurationId}", dto);
        }
        catch (SimulatorUnitNotFoundException)
        {
            return Results.NotFound();
        }
        catch (AircraftTypeNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateConfigurationAsync(
        Guid id, Guid configId, UpdateSimulatorConfigurationRequest body,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new UpdateSimulatorConfigurationCommand(
                id,
                configId,
                body.Name,
                body.AircraftTypeId,
                body.ConfigMode,
                body.SupportedTrainingTypes,
                body.MaxCapacityFlightDeck,
                body.MaxCapacityCabinCrew,
                body.IsActive), ct);

            return Results.Ok(dto);
        }
        catch (SimulatorConfigurationNotFoundException)
        {
            return Results.NotFound();
        }
        catch (AircraftTypeNotFoundException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ValidationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DeleteConfigurationAsync(
        Guid id,
        Guid configId,
        ISender sender,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteSimulatorConfigurationCommand(id, configId), ct);
            return Results.NoContent();
        }
        catch (SimulatorConfigurationNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

public record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);
