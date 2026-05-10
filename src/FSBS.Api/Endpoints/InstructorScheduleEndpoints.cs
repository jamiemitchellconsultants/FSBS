using FSBS.Application.InstructorSchedule.Commands;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Shared.InstructorSchedule;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Api.Endpoints;

public static class InstructorScheduleEndpoints
{
    public static IEndpointRouteBuilder MapInstructorScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/instructors")
            .WithTags("InstructorSchedule")
            .RequireAuthorization();

        // ── List (ScheduleAdmin / SystemAdmin) ────────────────────────────────
        group.MapGet("", async (FsbsDbContext db, CancellationToken ct) =>
            {
                var rows = await db.Instructors
                    .Include(i => i.User).ThenInclude(u => u.Profile)
                    .OrderBy(i => i.EmployeeNumber)
                    .Select(i => new InstructorRowDto(
                        i.Id,
                        i.EmployeeNumber,
                        (i.User.Profile != null ? (i.User.Profile.FirstName + " " + i.User.Profile.LastName) : i.User.Email),
                        i.User.Email))
                    .ToListAsync(ct);
                return Results.Ok(rows);
            })
            .WithName("ListInstructors")
            .RequireAuthorization("RequireStaff")
            .Produces<IReadOnlyList<InstructorRowDto>>();

        // ── Self ("me") routes ────────────────────────────────────────────────
        group.MapGet("/me/schedule", async (DateOnly from, DateOnly to, ISender s, CancellationToken ct) =>
                Results.Ok(await s.Send(new GetMyInstructorScheduleQuery(from, to), ct)))
            .WithName("GetMyInstructorSchedule")
            .Produces<InstructorScheduleDto>();

        group.MapPut("/me/schedule/pattern", async (
                WeeklyPatternUpsertRequest body, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                var result = await s.Send(new UpsertWeeklyPatternCommand(instructorId, body.Slots), ct);
                return Results.Ok(result);
            })
            .WithName("UpsertMyWeeklyPattern")
            .Produces<WeeklyPatternDto>();

        group.MapPut("/me/schedule/days/{date}", async (
                DateOnly date, SingleDayUpsertRequest body, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                await s.Send(new SetSingleDayCommand(instructorId, date, body.Available), ct);
                return Results.NoContent();
            })
            .WithName("SetMySingleDay");

        group.MapPost("/me/schedule/overrides", async (
                OverrideRequest body, ISender s, MeInstructorIdResolver me, HttpRequest http, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, null, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Created($"{http.Path}/{dto.Id}", dto);
            })
            .WithName("CreateMyOverride")
            .Produces<AvailabilityOverrideDto>(StatusCodes.Status201Created);

        group.MapPut("/me/schedule/overrides/{overrideId:guid}", async (
                Guid overrideId, OverrideRequest body, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, overrideId, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Ok(dto);
            })
            .WithName("UpdateMyOverride")
            .Produces<AvailabilityOverrideDto>();

        group.MapDelete("/me/schedule/overrides/{overrideId:guid}", async (
                Guid overrideId, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                await s.Send(new DeleteOverrideCommand(instructorId, overrideId), ct);
                return Results.NoContent();
            })
            .WithName("DeleteMyOverride");

        // ── /{instructorId}/ admin / cross-instructor routes ─────────────────
        group.MapGet("/{instructorId:guid}/schedule", async (
                Guid instructorId, DateOnly from, DateOnly to, ISender s, CancellationToken ct) =>
                Results.Ok(await s.Send(new GetInstructorScheduleQuery(instructorId, from, to), ct)))
            .WithName("GetInstructorSchedule")
            .Produces<InstructorScheduleDto>();

        group.MapPut("/{instructorId:guid}/schedule/pattern", async (
                Guid instructorId, WeeklyPatternUpsertRequest body, ISender s, CancellationToken ct) =>
            {
                var result = await s.Send(new UpsertWeeklyPatternCommand(instructorId, body.Slots), ct);
                return Results.Ok(result);
            })
            .WithName("UpsertInstructorWeeklyPattern")
            .Produces<WeeklyPatternDto>();

        group.MapPut("/{instructorId:guid}/schedule/days/{date}", async (
                Guid instructorId, DateOnly date, SingleDayUpsertRequest body, ISender s, CancellationToken ct) =>
            {
                await s.Send(new SetSingleDayCommand(instructorId, date, body.Available), ct);
                return Results.NoContent();
            })
            .WithName("SetInstructorSingleDay");

        group.MapPost("/{instructorId:guid}/schedule/overrides", async (
                Guid instructorId, OverrideRequest body, ISender s, HttpRequest http, CancellationToken ct) =>
            {
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, null, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Created($"{http.Path}/{dto.Id}", dto);
            })
            .WithName("CreateInstructorOverride")
            .Produces<AvailabilityOverrideDto>(StatusCodes.Status201Created);

        group.MapPut("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", async (
                Guid instructorId, Guid overrideId, OverrideRequest body, ISender s, CancellationToken ct) =>
            {
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, overrideId, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Ok(dto);
            })
            .WithName("UpdateInstructorOverride")
            .Produces<AvailabilityOverrideDto>();

        group.MapDelete("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", async (
                Guid instructorId, Guid overrideId, ISender s, CancellationToken ct) =>
            {
                await s.Send(new DeleteOverrideCommand(instructorId, overrideId), ct);
                return Results.NoContent();
            })
            .WithName("DeleteInstructorOverride");

        return app;
    }

    private static AvailabilityType ParseAvailabilityType(string raw) =>
        Enum.TryParse<AvailabilityType>(raw, ignoreCase: true, out var v)
            ? v
            : throw new BadHttpRequestException(
                $"Type must be one of {string.Join(", ", Enum.GetNames<AvailabilityType>())}.");
}

/// <summary>Lightweight row for the instructor picker.</summary>
public sealed record InstructorRowDto(Guid InstructorId, string EmployeeNumber, string FullName, string Email);

/// <summary>
/// Resolves the current user's instructor id at request time. Surfacing this
/// as a tiny scoped service lets minimal-API delegates inject it directly.
/// </summary>
public sealed class MeInstructorIdResolver(
    FSBS.Application.Common.Interfaces.IInstructorScheduleRepository repo,
    FSBS.Application.Common.Interfaces.ICurrentUser currentUser)
{
    public async Task<Guid> RequireAsync(CancellationToken ct) =>
        await repo.GetInstructorIdForUserAsync(currentUser.UserId, ct)
            ?? throw new FSBS.Application.Common.Exceptions.ForbiddenException(
                "Current user is not registered as an instructor.");
}
