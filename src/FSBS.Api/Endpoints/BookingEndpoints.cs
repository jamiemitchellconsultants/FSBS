using FSBS.Application.Bookings.Queries;
using FSBS.Application.Common.Exceptions;
using FSBS.Shared.Bookings;
using FSBS.Shared.Common;
using MediatR;

namespace FSBS.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/bookings")
            .WithTags("Bookings")
            .RequireAuthorization();

        group.MapGet("/", ListMyBookingsAsync)
            .WithName("GetMyBookings")
            .WithSummary("Return a cursor-paginated list of the current user's bookings.")
            .Produces<PagedResult<BookingSummaryDto>>();

        group.MapGet("/range", GetMyBookingsForRangeAsync)
            .WithName("GetMyBookingsForRange")
            .WithSummary("Return bookings with slots falling within a UTC date/time range.")
            .Produces<IReadOnlyList<BookingSummaryDto>>();

        group.MapGet("/{id:guid}", GetBookingDetailAsync)
            .WithName("GetBookingDetail")
            .WithSummary("Return full details for a single booking owned by the current user.")
            .Produces<BookingDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

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
}
