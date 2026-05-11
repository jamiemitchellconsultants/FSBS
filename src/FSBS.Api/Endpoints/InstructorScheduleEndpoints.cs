using FSBS.Application.InstructorSchedule.Commands;
using FSBS.Application.InstructorSchedule.Queries;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Shared.InstructorSchedule;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            .WithSummary("Return a roster of all active instructors with employee numbers and contact info (Staff only).")
            .RequireAuthorization("RequireStaff")
            .Produces<IReadOnlyList<InstructorRowDto>>();

        // ── Self ("me") routes ────────────────────────────────────────────────
        group.MapGet("/me/schedule", async (DateOnly from, DateOnly to, ISender s, CancellationToken ct) =>
                Results.Ok(await s.Send(new GetMyInstructorScheduleQuery(from, to), ct)))
            .WithName("GetMyInstructorSchedule")
            .WithSummary("Return the current instructor's schedule (weekly pattern + overrides) for a date range.")
            .Produces<InstructorScheduleDto>();

        group.MapPut("/me/schedule/pattern", async (
                WeeklyPatternUpsertRequest body, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                var result = await s.Send(new UpsertWeeklyPatternCommand(instructorId, body.Slots), ct);
                return Results.Ok(result);
            })
            .WithName("UpsertMyWeeklyPattern")
            .WithSummary("Set or update the current instructor's weekly standard availability pattern.")
            .Produces<WeeklyPatternDto>();

        group.MapPut("/me/schedule/days/{date}", async (
                DateOnly date, SingleDayUpsertRequest body, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                await s.Send(new SetSingleDayCommand(instructorId, date, body.Available), ct);
                return Results.NoContent();
            })
            .WithName("SetMySingleDay")
            .WithSummary("Override the current instructor's availability for a single day (Available/Unavailable).");

        group.MapPost("/me/schedule/overrides", async (
                OverrideRequest body, ISender s, MeInstructorIdResolver me, HttpRequest http, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, null, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Created($"{http.Path}/{dto.Id}", dto);
            })
            .WithName("CreateMyOverride")
            .WithSummary("Create a time-bounded availability override (vacation, illness, meeting, other).")
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
            .WithSummary("Update an existing availability override (type, range, notes).")
            .Produces<AvailabilityOverrideDto>();

        group.MapDelete("/me/schedule/overrides/{overrideId:guid}", async (
                Guid overrideId, ISender s, MeInstructorIdResolver me, CancellationToken ct) =>
            {
                var instructorId = await me.RequireAsync(ct);
                await s.Send(new DeleteOverrideCommand(instructorId, overrideId), ct);
                return Results.NoContent();
            })
            .WithName("DeleteMyOverride")
            .WithSummary("Delete an availability override.");

        // ── /{instructorId}/ admin / cross-instructor routes ─────────────────
        group.MapGet("/{instructorId:guid}/schedule", async (
                Guid instructorId, DateOnly from, DateOnly to, ISender s, CancellationToken ct) =>
                Results.Ok(await s.Send(new GetInstructorScheduleQuery(instructorId, from, to), ct)))
            .WithName("GetInstructorSchedule")
            .WithSummary("Return an instructor's schedule by ID for a date range (Staff/Admins only).")
            .Produces<InstructorScheduleDto>();

        group.MapPut("/{instructorId:guid}/schedule/pattern", async (
                Guid instructorId, WeeklyPatternUpsertRequest body, ISender s, CancellationToken ct) =>
            {
                var result = await s.Send(new UpsertWeeklyPatternCommand(instructorId, body.Slots), ct);
                return Results.Ok(result);
            })
            .WithName("UpsertInstructorWeeklyPattern")
            .WithSummary("Set or update an instructor's weekly standard availability pattern (Admins only).")
            .Produces<WeeklyPatternDto>();

        group.MapPut("/{instructorId:guid}/schedule/days/{date}", async (
                Guid instructorId, DateOnly date, SingleDayUpsertRequest body, ISender s, CancellationToken ct) =>
            {
                await s.Send(new SetSingleDayCommand(instructorId, date, body.Available), ct);
                return Results.NoContent();
            })
            .WithName("SetInstructorSingleDay")
            .WithSummary("Override an instructor's availability for a single day (Admins only).");

        group.MapPost("/{instructorId:guid}/schedule/overrides", async (
                Guid instructorId, OverrideRequest body, ISender s, HttpRequest http, CancellationToken ct) =>
            {
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, null, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Created($"{http.Path}/{dto.Id}", dto);
            })
            .WithName("CreateInstructorOverride")
            .WithSummary("Create a time-bounded availability override for an instructor (Admins only).")
            .Produces<AvailabilityOverrideDto>(StatusCodes.Status201Created);

        group.MapPut("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", async (
                Guid instructorId, Guid overrideId, OverrideRequest body, ISender s, CancellationToken ct) =>
            {
                var type = ParseAvailabilityType(body.Type);
                var dto = await s.Send(new UpsertOverrideCommand(instructorId, overrideId, body.StartAt, body.EndAt, type, body.Notes), ct);
                return Results.Ok(dto);
            })
            .WithName("UpdateInstructorOverride")
            .WithSummary("Update an instructor's availability override (Admins only).")
            .Produces<AvailabilityOverrideDto>();

        group.MapDelete("/{instructorId:guid}/schedule/overrides/{overrideId:guid}", async (
                Guid instructorId, Guid overrideId, ISender s, CancellationToken ct) =>
            {
                await s.Send(new DeleteOverrideCommand(instructorId, overrideId), ct);
                return Results.NoContent();
            })
            .WithName("DeleteInstructorOverride")
            .WithSummary("Delete an instructor's availability override (Admins only).");

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
