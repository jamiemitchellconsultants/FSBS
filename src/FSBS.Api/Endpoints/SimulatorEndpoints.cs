using FSBS.Application.Common.Interfaces;
using FSBS.Application.Simulators.Queries;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;

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
        ISimulatorRepository repo, CancellationToken ct)
    {
        var items = await repo.ListAllAsync(ct);
        return Results.Ok(new SimulatorListResponse(items.Select(MapToDetail).ToList()));
    }

    private static async Task<IResult> GetSimulatorDetailAsync(
        Guid id, ISimulatorRepository repo, CancellationToken ct)
    {
        var unit = await repo.FindByIdAsync(id, ct);
        return unit is null ? Results.NotFound() : Results.Ok(MapToDetail(unit));
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
        CreateSimulatorUnitRequest body, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return Results.Problem(detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);
        if (body.DefaultReconfigMins <= 0)
            return Results.Problem(detail: "DefaultReconfigMins must be greater than 0.", statusCode: StatusCodes.Status400BadRequest);

        var unit = new SimulatorUnit
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            FstdLevel = body.FstdLevel.Trim(),
            Manufacturer = body.Manufacturer?.Trim(),
            Location = body.Location?.Trim(),
            DefaultReconfigMins = body.DefaultReconfigMins,
            IsActive = true,
        };

        db.SimulatorUnits.Add(unit);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/simulators/{unit.Id}", MapToDetail(unit));
    }

    private static async Task<IResult> UpdateUnitAsync(
        Guid id, UpdateSimulatorUnitRequest body, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var unit = await repo.FindByIdAsync(id, ct);
        if (unit is null) return Results.NotFound();

        unit.Name = body.Name.Trim();
        unit.FstdLevel = body.FstdLevel.Trim();
        unit.Manufacturer = body.Manufacturer?.Trim();
        unit.Location = body.Location?.Trim();
        unit.DefaultReconfigMins = body.DefaultReconfigMins;
        unit.IsActive = body.IsActive;

        await db.SaveChangesAsync(ct);
        return Results.Ok(MapToDetail(unit));
    }

    private static async Task<IResult> DeleteUnitAsync(
        Guid id, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var unit = await repo.FindByIdAsync(id, ct);
        if (unit is null) return Results.NotFound();

        unit.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── SimulatorBay write handlers ───────────────────────────────────────────

    private static async Task<IResult> CreateBayAsync(
        Guid id, CreateSimulatorBayRequest body, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var unit = await repo.FindByIdAsync(id, ct);
        if (unit is null) return Results.NotFound();

        var bay = new SimulatorBay
        {
            Id = Guid.NewGuid(),
            SimulatorUnitId = id,
            BayCode = body.BayCode.Trim(),
            Description = body.Description?.Trim(),
            Status = BayStatus.Operational,
        };

        db.SimulatorBays.Add(bay);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/v1/simulators/{id}/bays/{bay.Id}", MapBay(bay));
    }

    private static async Task<IResult> UpdateBayAsync(
        Guid id, Guid bayId, UpdateSimulatorBayRequest body, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var bay = await repo.FindBayAsync(bayId, ct);
        if (bay is null || bay.SimulatorUnitId != id) return Results.NotFound();

        bay.BayCode = body.BayCode.Trim();
        bay.Description = body.Description?.Trim();
        bay.Status = Enum.Parse<BayStatus>(body.Status);

        await db.SaveChangesAsync(ct);
        return Results.Ok(MapBay(bay));
    }

    private static async Task<IResult> DeleteBayAsync(
        Guid id, Guid bayId, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var bay = await repo.FindBayAsync(bayId, ct);
        if (bay is null || bay.SimulatorUnitId != id) return Results.NotFound();

        bay.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── SimulatorConfiguration write handlers ─────────────────────────────────

    private static async Task<IResult> CreateConfigurationAsync(
        Guid id, CreateSimulatorConfigurationRequest body, ISimulatorRepository repo,
        IAircraftTypeRepository aircraftTypeRepo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var unit = await repo.FindByIdAsync(id, ct);
        if (unit is null) return Results.NotFound();

        var aircraftType = await aircraftTypeRepo.FindByIdAsync(body.AircraftTypeId, ct);
        if (aircraftType is null)
            return Results.Problem(detail: $"AircraftType '{body.AircraftTypeId}' not found.", statusCode: StatusCodes.Status400BadRequest);

        if (!TryParseTrainingTypes(body.SupportedTrainingTypes, out var trainingTypes, out var parseError))
            return Results.Problem(detail: parseError, statusCode: StatusCodes.Status400BadRequest);

        if (!Enum.TryParse<ConfigurationMode>(body.ConfigMode, out var configMode))
            return Results.Problem(detail: $"Invalid ConfigMode '{body.ConfigMode}'.", statusCode: StatusCodes.Status400BadRequest);

        var config = new SimulatorConfiguration
        {
            Id = Guid.NewGuid(),
            SimulatorUnitId = id,
            Name = body.Name.Trim(),
            AircraftTypeId = body.AircraftTypeId,
            ConfigMode = configMode,
            SupportedTrainingTypes = trainingTypes,
            MaxCapacityFlightDeck = body.MaxCapacityFlightDeck,
            MaxCapacityCabinCrew = body.MaxCapacityCabinCrew,
            IsActive = true,
        };

        db.SimulatorConfigurations.Add(config);
        await db.SaveChangesAsync(ct);
        config.AircraftType = aircraftType;
        return Results.Created($"/v1/simulators/{id}/configurations/{config.Id}", MapConfig(config));
    }

    private static async Task<IResult> UpdateConfigurationAsync(
        Guid id, Guid configId, UpdateSimulatorConfigurationRequest body,
        ISimulatorRepository repo, IAircraftTypeRepository aircraftTypeRepo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var config = await repo.FindConfigurationAsync(configId, ct);
        if (config is null || config.SimulatorUnitId != id) return Results.NotFound();

        var aircraftType = await aircraftTypeRepo.FindByIdAsync(body.AircraftTypeId, ct);
        if (aircraftType is null)
            return Results.Problem(detail: $"AircraftType '{body.AircraftTypeId}' not found.", statusCode: StatusCodes.Status400BadRequest);

        if (!TryParseTrainingTypes(body.SupportedTrainingTypes, out var trainingTypes, out var parseError))
            return Results.Problem(detail: parseError, statusCode: StatusCodes.Status400BadRequest);

        if (!Enum.TryParse<ConfigurationMode>(body.ConfigMode, out var configMode))
            return Results.Problem(detail: $"Invalid ConfigMode '{body.ConfigMode}'.", statusCode: StatusCodes.Status400BadRequest);

        config.Name = body.Name.Trim();
        config.AircraftTypeId = body.AircraftTypeId;
        config.AircraftType = aircraftType;
        config.ConfigMode = configMode;
        config.SupportedTrainingTypes = trainingTypes;
        config.MaxCapacityFlightDeck = body.MaxCapacityFlightDeck;
        config.MaxCapacityCabinCrew = body.MaxCapacityCabinCrew;
        config.IsActive = body.IsActive;

        await db.SaveChangesAsync(ct);
        return Results.Ok(MapConfig(config));
    }

    private static async Task<IResult> DeleteConfigurationAsync(
        Guid id, Guid configId, ISimulatorRepository repo,
        FSBS.Infrastructure.Persistence.FsbsDbContext db, CancellationToken ct)
    {
        var config = await repo.FindConfigurationAsync(configId, ct);
        if (config is null || config.SimulatorUnitId != id) return Results.NotFound();

        config.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static SimulatorDetailDto MapToDetail(SimulatorUnit unit) =>
        new(
            UnitId: unit.Id,
            Name: unit.Name,
            FstdLevel: unit.FstdLevel,
            Manufacturer: unit.Manufacturer,
            Location: unit.Location,
            DefaultReconfigMins: unit.DefaultReconfigMins,
            IsActive: unit.IsActive,
            Bays: unit.Bays
                .Where(b => !b.IsDeleted)
                .Select(MapBay)
                .ToList(),
            Configurations: unit.Configurations
                .Where(c => !c.IsDeleted)
                .Select(MapConfig)
                .ToList());

    private static SimulatorBayDto MapBay(SimulatorBay b) =>
        new(b.Id, b.BayCode, b.Status.ToString());

    private static SimulatorConfigurationDto MapConfig(SimulatorConfiguration c) =>
        new(c.Id, c.Name,
            c.AircraftTypeId,
            c.AircraftType?.IcaoCode ?? string.Empty,
            c.AircraftType?.Name ?? string.Empty,
            c.ConfigMode.ToString(),
            c.SupportedTrainingTypes.Select(t => t.ToString()).ToList(),
            c.MaxCapacityFlightDeck, c.MaxCapacityCabinCrew, c.IsActive);

    private static bool TryParseTrainingTypes(
        IReadOnlyList<string> raw, out List<TrainingType> result, out string error)
    {
        result = [];
        error = string.Empty;
        foreach (var s in raw)
        {
            if (!Enum.TryParse<TrainingType>(s, out var t))
            {
                error = $"Invalid TrainingType '{s}'.";
                return false;
            }
            result.Add(t);
        }
        if (result.Count == 0)
        {
            error = "At least one SupportedTrainingType is required.";
            return false;
        }
        return true;
    }
}

public record SimulatorListResponse(IReadOnlyList<SimulatorDetailDto> Items);
