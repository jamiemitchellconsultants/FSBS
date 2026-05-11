using FSBS.Application.Common.Interfaces;
using FSBS.Application.InstructorSchedule.Commands;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Domain.Enums;
using FSBS.Shared.InstructorSchedule;
using MediatR;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for instructor schedule management (weekly patterns, daily overrides, availability).
/// Routes are under <c>/v1/instructors</c> and require authentication.
/// Instructors manage their own schedules under the <c>/me/schedule</c> prefix.
/// Admins (ScheduleAdmin, SystemAdmin) can view and manage any instructor's schedule.
/// </summary>
public static class InstructorScheduleEndpoints
{
    public static IEndpointRouteBuilder MapInstructorScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/instructors")
            .WithTags("InstructorSchedule")
            .RequireAuthorization();

        // ── List (Staff only) ─────────────────────────────────────────────────
        group.MapGet("", ListInstructorsAsync)
            .WithName("ListInstructors")
            .WithSummary("Return a roster of all active instructors with employee numbers and contact info (Staff only).")
            .RequireAuthorization("RequireStaff")
            .Produces<IReadOnlyList<InstructorRowDto>>();

        // ── Self ("me") routes ────────────────────────────────────────────────
        group.MapGet("/me/schedule", GetMyInstructorScheduleAsync)
            .WithName("GetMyInstructorSchedule")
            .WithSummary("Return the current instructor's schedule (weekly pattern + overrides) for a date range.")
            .RequireAuthorization("RequireInstructor")
            .Produces<InstructorScheduleDto>()
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/me/schedule/pattern", UpsertMyWeeklyPatternAsync)
            .WithName("UpsertMyWeeklyPattern")
            .WithSummary("Set or update the current instructor's weekly standard availability pattern.")
            .RequireAuthorization("RequireInstructor")
            .Produces<WeeklyPatternDto>()
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/me/schedule/days/{date}", SetMySingleDayAsync)
            .WithName("SetMySingleDay")
            .WithSummary("Override the current instructor's availability for a single day (Available/Unavailable).")
            .RequireAuthorization("RequireInstructor")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/me/schedule/overrides", CreateMyOverrideAsync)
            .WithName("CreateMyOverride")
            .WithSummary("Create a time-bounded availability override (vacation, illness, meeting, other).")
            .RequireAuthorization("RequireInstructor")
            .Produces<AvailabilityOverrideDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/me/schedule/overrides/{overrideId:guid}", UpdateMyOverrideAsync)
            .WithName("UpdateMyOverride")
            .WithSummary("Update an existing availability override (type, range, notes).")
            .RequireAuthorization("RequireInstructor")
            .Produces<AvailabilityOverrideDto>()
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/me/schedule/overrides/{overrideId:guid}", DeleteMyOverrideAsync)
            .WithName("DeleteMyOverride")
            .WithSummary("Delete an availability override.")
            .RequireAuthorization("RequireInstructor")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden);

        // ── /{instructorId}/ admin / cross-instructor routes ─────────────────
        group.MapGet("/{instructorId:guid}/schedule", GetInstructorScheduleAsync)
            .WithName("GetInstructorSchedule")
            .WithSummary("Return an instructor's schedule by ID for a date range (Staff/Admins only).")
            .RequireAuthorization("RequireStaff")
            .Produces<InstructorScheduleDto>();

        group.MapPut("/{instructorId:guid}/schedule/pattern", UpsertInstructorWeeklyPatternAsync)
            .WithName("UpsertInstructorWeeklyPattern")
            .WithSummary("Set or update an instructor's weekly standard availability pattern (Admins only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<WeeklyPatternDto>();

        group.MapPut("/{instructorId:guid}/schedule/days/{date}", SetInstructorSingleDayAsync)
            .WithName("SetInstructorSingleDay")
            .WithSummary("Override an instructor's availability for a single day (Admins only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/{instructorId:guid}/schedule/overrides", CreateInstructorOverrideAsync)
            .WithName("CreateInstructorOverride")
            .WithSummary("Create a time-bounded availability override for an instructor (Admins only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AvailabilityOverrideDto>(StatusCodes.Status201Created);

        group.MapPut("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", UpdateInstructorOverrideAsync)
            .WithName("UpdateInstructorOverride")
            .WithSummary("Update an instructor's availability override (Admins only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces<AvailabilityOverrideDto>();

        group.MapDelete("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", DeleteInstructorOverrideAsync)
            .WithName("DeleteInstructorOverride")
            .WithSummary("Delete an instructor's availability override (Admins only).")
            .RequireAuthorization("RequireScheduleAdmin")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    // ── Read handlers ─────────────────────────────────────────────────────────

    private static async Task<IResult> ListInstructorsAsync(
        ISender sender,
        CancellationToken ct)
    {
        var rows = await sender.Send(new ListInstructorsQuery(), ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> GetMyInstructorScheduleAsync(
        DateOnly from,
        DateOnly to,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetMyInstructorScheduleQuery(from, to), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetInstructorScheduleAsync(
        Guid instructorId,
        DateOnly from,
        DateOnly to,
        ISender sender,
        CancellationToken ct)
    {
        var dto = await sender.Send(new GetInstructorScheduleQuery(instructorId, from, to), ct);
        return Results.Ok(dto);
    }

    // ── Write handlers ────────────────────────────────────────────────────────

    private static async Task<IResult> UpsertMyWeeklyPatternAsync(
        WeeklyPatternUpsertRequest body,
        ISender sender,
        MeInstructorIdResolver me,
        CancellationToken ct)
    {
        var instructorId = await me.ResolveAsync(ct);
        if (instructorId == null)
            return Results.Forbid();

        var result = await sender.Send(new UpsertWeeklyPatternCommand(instructorId.Value, body.Slots), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SetMySingleDayAsync(
        DateOnly date,
        SingleDayUpsertRequest body,
        ISender sender,
        MeInstructorIdResolver me,
        CancellationToken ct)
    {
        var instructorId = await me.ResolveAsync(ct);
        if (instructorId == null)
            return Results.Forbid();

        await sender.Send(new SetSingleDayCommand(instructorId.Value, date, body.Available), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateMyOverrideAsync(
        OverrideRequest body,
        ISender sender,
        MeInstructorIdResolver me,
        HttpRequest http,
        CancellationToken ct)
    {
        var instructorId = await me.ResolveAsync(ct);
        if (instructorId == null)
            return Results.Forbid();

        var type = ParseAvailabilityType(body.Type);
        var dto = await sender.Send(new UpsertOverrideCommand(instructorId.Value, null, body.StartAt, body.EndAt, type, body.Notes), ct);
        return Results.Created($"{http.Path}/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateMyOverrideAsync(
        Guid overrideId,
        OverrideRequest body,
        ISender sender,
        MeInstructorIdResolver me,
        CancellationToken ct)
    {
        var instructorId = await me.ResolveAsync(ct);
        if (instructorId == null)
            return Results.Forbid();

        var type = ParseAvailabilityType(body.Type);
        var dto = await sender.Send(new UpsertOverrideCommand(instructorId.Value, overrideId, body.StartAt, body.EndAt, type, body.Notes), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteMyOverrideAsync(
        Guid overrideId,
        ISender sender,
        MeInstructorIdResolver me,
        CancellationToken ct)
    {
        var instructorId = await me.ResolveAsync(ct);
        if (instructorId == null)
            return Results.Forbid();

        await sender.Send(new DeleteOverrideCommand(instructorId.Value, overrideId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpsertInstructorWeeklyPatternAsync(
        Guid instructorId,
        WeeklyPatternUpsertRequest body,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpsertWeeklyPatternCommand(instructorId, body.Slots), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SetInstructorSingleDayAsync(
        Guid instructorId,
        DateOnly date,
        SingleDayUpsertRequest body,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new SetSingleDayCommand(instructorId, date, body.Available), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateInstructorOverrideAsync(
        Guid instructorId,
        OverrideRequest body,
        ISender sender,
        HttpRequest http,
        CancellationToken ct)
    {
        var type = ParseAvailabilityType(body.Type);
        var dto = await sender.Send(new UpsertOverrideCommand(instructorId, null, body.StartAt, body.EndAt, type, body.Notes), ct);
        return Results.Created($"{http.Path}/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateInstructorOverrideAsync(
        Guid instructorId,
        Guid overrideId,
        OverrideRequest body,
        ISender sender,
        CancellationToken ct)
    {
        var type = ParseAvailabilityType(body.Type);
        var dto = await sender.Send(new UpsertOverrideCommand(instructorId, overrideId, body.StartAt, body.EndAt, type, body.Notes), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteInstructorOverrideAsync(
        Guid instructorId,
        Guid overrideId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteOverrideCommand(instructorId, overrideId), ct);
        return Results.NoContent();
    }

    private static AvailabilityType ParseAvailabilityType(string raw) =>
        Enum.TryParse<AvailabilityType>(raw, ignoreCase: true, out var v)
            ? v
            : throw new BadHttpRequestException(
                $"Type must be one of {string.Join(", ", Enum.GetNames<AvailabilityType>())}.");
}

/// <summary>
/// Resolves the current user's instructor ID at request time. Surfacing this
/// as a tiny scoped service lets minimal-API delegates inject it directly.
/// Returns null if the current user is not registered as an instructor,
/// allowing the endpoint handler to return a 403 Forbidden response.
/// </summary>
public sealed class MeInstructorIdResolver(
    IInstructorScheduleRepository repo,
    ICurrentUser currentUser)
{
    /// <summary>
    /// Resolves the current user's instructor ID, or returns null if they
    /// are not registered as an instructor. Endpoints can then return
    /// <see cref="Results.Forbid()"/> if null is returned.
    /// </summary>
    public async Task<Guid?> ResolveAsync(CancellationToken ct) =>
        await repo.GetInstructorIdForUserAsync(currentUser.UserId, ct);
}
