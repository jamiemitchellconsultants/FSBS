using FSBS.Application.ReferenceData.Commands;
using FSBS.Application.ReferenceData.Queries;
using FSBS.Shared.ReferenceData;
using MediatR;

namespace FSBS.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/reference-data")
            .WithTags("ReferenceData")
            .RequireAuthorization();

        // ── Customer Classes ──────────────────────────────────────────────────
        group.MapGet("/customer-classes", async (ISender s, CancellationToken ct) =>
            Results.Ok(await s.Send(new GetCustomerClassesQuery(), ct)))
            .WithName("GetCustomerClasses")
            .Produces<IReadOnlyList<ReferenceItemDto>>();

        group.MapPut("/customer-classes/{code}", async (string code, UpsertReferenceItemRequest body, ISender s, CancellationToken ct) =>
        {
            if (code != body.Code) return Results.Problem("Route code must match body code.", statusCode: StatusCodes.Status400BadRequest);
            var result = await s.Send(new UpsertCustomerClassCommand(body), ct);
            return Results.Ok(result);
        })
        .WithName("UpsertCustomerClass")
        .RequireAuthorization("RequireSystemAdmin")
        .Produces<ReferenceItemDto>();

        // ── Discount Types ────────────────────────────────────────────────────
        group.MapGet("/discount-types", async (ISender s, CancellationToken ct) =>
            Results.Ok(await s.Send(new GetDiscountTypesQuery(), ct)))
            .WithName("GetDiscountTypes")
            .Produces<IReadOnlyList<ReferenceItemDto>>();

        group.MapPut("/discount-types/{code}", async (string code, UpsertReferenceItemRequest body, ISender s, CancellationToken ct) =>
        {
            if (code != body.Code) return Results.Problem("Route code must match body code.", statusCode: StatusCodes.Status400BadRequest);
            var result = await s.Send(new UpsertDiscountTypeCommand(body), ct);
            return Results.Ok(result);
        })
        .WithName("UpsertDiscountType")
        .RequireAuthorization("RequireSystemAdmin")
        .Produces<ReferenceItemDto>();

        // ── Payment Methods ───────────────────────────────────────────────────
        group.MapGet("/payment-methods", async (ISender s, CancellationToken ct) =>
            Results.Ok(await s.Send(new GetPaymentMethodsQuery(), ct)))
            .WithName("GetPaymentMethods")
            .Produces<IReadOnlyList<ReferenceItemDto>>();

        group.MapPut("/payment-methods/{code}", async (string code, UpsertReferenceItemRequest body, ISender s, CancellationToken ct) =>
        {
            if (code != body.Code) return Results.Problem("Route code must match body code.", statusCode: StatusCodes.Status400BadRequest);
            var result = await s.Send(new UpsertPaymentMethodCommand(body), ct);
            return Results.Ok(result);
        })
        .WithName("UpsertPaymentMethod")
        .RequireAuthorization("RequireSystemAdmin")
        .Produces<ReferenceItemDto>();

        // ── Account Statuses ──────────────────────────────────────────────────
        group.MapGet("/account-statuses", async (ISender s, CancellationToken ct) =>
            Results.Ok(await s.Send(new GetAccountStatusesQuery(), ct)))
            .WithName("GetAccountStatuses")
            .Produces<IReadOnlyList<AccountStatusDto>>();

        group.MapPut("/account-statuses/{code}", async (string code, UpsertAccountStatusRequest body, ISender s, CancellationToken ct) =>
        {
            if (code != body.Code) return Results.Problem("Route code must match body code.", statusCode: StatusCodes.Status400BadRequest);
            var result = await s.Send(new UpsertAccountStatusCommand(body), ct);
            return Results.Ok(result);
        })
        .WithName("UpsertAccountStatus")
        .RequireAuthorization("RequireSystemAdmin")
        .Produces<AccountStatusDto>();

        return app;
    }
}
