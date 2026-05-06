using FSBS.Api.Hubs;
using FSBS.Application.Bookings.Commands;
using FSBS.Application.Bookings.Queries;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Domain.Exceptions;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FSBS.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/bookings")
            .WithTags("Bookings")
            .RequireAuthorization();

        // ── Read endpoints ────────────────────────────────────────────────────

        group.MapGet("/", ListMyBookingsAsync)
            .WithName("GetMyBookings")
            .WithSummary("Return a cursor-paginated list of the current user's bookings.")
            .Produces<PagedResult<BookingSummaryDto>>();

        group.MapGet("/range", GetMyBookingsForRangeAsync)
            .WithName("GetMyBookingsForRange")
            .WithSummary("Return bookings with slots falling within a UTC date/time range.")
            .Produces<IReadOnlyList<BookingSummaryDto>>();

        group.MapGet("/pending-approval", GetPendingApprovalAsync)
            .WithName("GetPendingApprovalBookings")
            .WithSummary("Return all bookings awaiting SalesStaff approval (InternalStudent flow).")
            .RequireAuthorization("RequireApprover")
            .Produces<IReadOnlyList<BookingSummaryDto>>();

        group.MapGet("/{id:guid}", GetBookingDetailAsync)
            .WithName("GetBookingDetail")
            .WithSummary("Return full details for a single booking owned by the current user.")
            .Produces<BookingDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        // ── Write endpoints ───────────────────────────────────────────────────

        group.MapPost("/", CreateBookingAsync)
            .WithName("CreateBooking")
            .WithSummary("Reserve a simulator bay slot. Idempotency-Key header required.")
            .Produces<BookSimulatorSlotResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/approve", ApproveBookingAsync)
            .WithName("ApproveBooking")
            .WithSummary("Approve a PendingApproval booking (SalesStaff / SystemAdmin only).")
            .RequireAuthorization("RequireApprover")
            .Produces<ApproveBookingResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/reject", RejectBookingAsync)
            .WithName("RejectBooking")
            .WithSummary("Reject a PendingApproval booking with a mandatory reason (SalesStaff / SystemAdmin only).")
            .RequireAuthorization("RequireApprover")
            .Produces<RejectBookingResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/cancel", CancelBookingAsync)
            .WithName("CancelBooking")
            .WithSummary("Cancel a booking. Status becomes CancelledByCustomer or CancelledByAdmin based on caller role.")
            .Produces<CancelBookingResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ── Read handlers ─────────────────────────────────────────────────────────

    private static async Task<IResult> ListMyBookingsAsync(
        ISender sender,
        string? after,
        int limit,
        CancellationToken ct)
    {
        limit = Math.Clamp(limit == 0 ? 20 : limit, 1, 100);
        var result = await sender.Send(new GetMyBookingsQuery(after, limit), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMyBookingsForRangeAsync(
        ISender sender,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var items = await sender.Send(new GetMyBookingsForRangeQuery(from, to), ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetPendingApprovalAsync(
        ISender sender,
        CancellationToken ct)
    {
        var items = await sender.Send(new GetPendingApprovalBookingsQuery(), ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetBookingDetailAsync(
        ISender sender,
        Guid id,
        CancellationToken ct)
    {
        try
        {
            var dto = await sender.Send(new GetBookingDetailQuery(id), ct);
            return Results.Ok(dto);
        }
        catch (BookingNotFoundException)
        {
            return Results.NotFound();
        }
    }

    // ── Write handlers ────────────────────────────────────────────────────────

    private static async Task<IResult> CreateBookingAsync(
        ISender sender,
        IAvailabilityCache availabilityCache,
        IAvailabilityReadService availabilityReadService,
        IHubContext<AvailabilityHub> hubContext,
        HttpRequest request,
        CreateBookingRequest body,
        CancellationToken ct)
    {
        if (!request.Headers.TryGetValue("Idempotency-Key", out var keyHeader)
            || !Guid.TryParse(keyHeader, out var idempotencyKey))
        {
            return Results.Problem(
                detail: "A valid UUID 'Idempotency-Key' header is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new BookSimulatorSlotCommand(
            BayId: body.BayId,
            ConfigurationId: body.ConfigurationId,
            TrainingType: body.TrainingType,
            SlotStart: body.SlotStart,
            SlotEnd: body.SlotEnd,
            StudentCount: body.StudentCount,
            IdempotencyKey: idempotencyKey,
            InstructorId: body.InstructorId,
            DepartmentName: body.DepartmentName,
            BudgetCode: body.BudgetCode);

        try
        {
            var result = await sender.Send(command, ct);
            await PushAvailabilityUpdateAsync(
                body.SimulatorId, availabilityCache, availabilityReadService, hubContext, ct);
            return Results.Created($"/v1/bookings/{result.BookingId}", result);
        }
        catch (BookingConflictException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (DomainException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ApproveBookingAsync(
        ISender sender,
        IAvailabilityCache availabilityCache,
        IAvailabilityReadService availabilityReadService,
        IHubContext<AvailabilityHub> hubContext,
        Guid id,
        ApproveBookingRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new ApproveBookingCommand(id), ct);
            await PushAvailabilityUpdateAsync(
                body.SimulatorId, availabilityCache, availabilityReadService, hubContext, ct);
            return Results.Ok(result);
        }
        catch (BookingNotFoundException)
        {
            return Results.NotFound();
        }
        catch (SelfApprovalException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (InvalidBookingStateTransitionException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> RejectBookingAsync(
        ISender sender,
        IAvailabilityCache availabilityCache,
        IAvailabilityReadService availabilityReadService,
        IHubContext<AvailabilityHub> hubContext,
        Guid id,
        RejectBookingRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Reason) || body.Reason.Length < 10)
        {
            return Results.Problem(
                detail: "Rejection reason must be at least 10 characters.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await sender.Send(new RejectBookingCommand(id, body.Reason), ct);
            await PushAvailabilityUpdateAsync(
                body.SimulatorId, availabilityCache, availabilityReadService, hubContext, ct);
            return Results.Ok(result);
        }
        catch (BookingNotFoundException)
        {
            return Results.NotFound();
        }
        catch (SelfApprovalException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (InvalidBookingStateTransitionException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CancelBookingAsync(
        ISender sender,
        IAvailabilityCache availabilityCache,
        IAvailabilityReadService availabilityReadService,
        IHubContext<AvailabilityHub> hubContext,
        Guid id,
        CancelBookingRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new CancelBookingCommand(id, body.Reason), ct);
            await PushAvailabilityUpdateAsync(
                body.SimulatorId, availabilityCache, availabilityReadService, hubContext, ct);
            return Results.Ok(result);
        }
        catch (BookingNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidBookingStateTransitionException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // ── Shared helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Invalidates the Redis availability cache for the simulator and pushes
    /// a fresh grid to all SignalR clients subscribed to that simulator.
    /// </summary>
    private static async Task PushAvailabilityUpdateAsync(
        Guid simulatorId,
        IAvailabilityCache cache,
        IAvailabilityReadService readService,
        IHubContext<AvailabilityHub> hubContext,
        CancellationToken ct)
    {
        await cache.InvalidateAsync(simulatorId, ct);
        var from = DateTimeOffset.UtcNow;
        var to   = from.AddDays(30);
        var grid = await readService.GetAvailabilityAsync(simulatorId, from, to, ct);
        await cache.SetAsync(simulatorId, from, to, grid, ct);
        await AvailabilityHub.SendAvailabilityUpdatedAsync(hubContext, simulatorId, grid, ct);
    }
}

// ── Request body records ──────────────────────────────────────────────────────

/// <summary>Request body for POST /bookings.</summary>
public record CreateBookingRequest(
    /// <summary>The simulator that owns the bay — used for cache invalidation and SignalR push.</summary>
    Guid SimulatorId,
    Guid BayId,
    Guid ConfigurationId,
    TrainingType TrainingType,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    int StudentCount,
    Guid? InstructorId = null,
    string? DepartmentName = null,
    string? BudgetCode = null);

/// <summary>Request body for PUT /bookings/{id}/approve.</summary>
public record ApproveBookingRequest(
    /// <summary>The simulator that owns the booking's bay — used for cache invalidation and SignalR push.</summary>
    Guid SimulatorId);

/// <summary>Request body for PUT /bookings/{id}/reject.</summary>
public record RejectBookingRequest(
    Guid SimulatorId,
    string Reason);

/// <summary>Request body for PUT /bookings/{id}/cancel.</summary>
public record CancelBookingRequest(
    Guid SimulatorId,
    string? Reason = null);
