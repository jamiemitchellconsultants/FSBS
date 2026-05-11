using FSBS.Application.Organisations.Commands;
using FSBS.Application.Organisations.Queries;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Shared.Payments;
using MediatR;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for listing and looking up organisations.
/// Routes are under <c>/v1/organisations</c> and require authentication.
/// </summary>
public static class OrganisationEndpoints
{
    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/organisations")
            .WithTags("Organisations")
            .RequireAuthorization();

        group.MapGet("/", ListAsync)
            .WithName("ListOrganisations")
            .WithSummary("Return a name-sorted list of all active organisations.")
            .Produces<OrganisationListResponse>(StatusCodes.Status200OK);

        group.MapGet("/{orgId:guid}/account", GetAccountAsync)
            .WithName("GetOrgAccount")
            .WithSummary("Return the account summary and recent payments for an organisation.")
            .RequireAuthorization("RequireApprover")
            .Produces<OrgAccountDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{orgId:guid}/payments", RecordPaymentAsync)
            .WithName("RecordOrgPayment")
            .WithSummary("Record a pre-payment on account made by an organisation (SalesStaff / SystemAdmin).")
            .RequireAuthorization("RequireApprover")
            .Produces<PaymentDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(ISender sender, CancellationToken ct)
    {
        var items = await sender.Send(new ListOrganisationsQuery(), ct);
        return Results.Ok(new OrganisationListResponse(items));
    }

    private static async Task<IResult> GetAccountAsync(
        Guid orgId, ISender sender, CancellationToken ct)
    {
        var account = await sender.Send(new GetOrganisationAccountQuery(orgId), ct);
        return account is null ? Results.NotFound() : Results.Ok(account);
    }

    private static async Task<IResult> RecordPaymentAsync(
        Guid orgId,
        RecordPaymentRequest body,
        ISender sender,
        CancellationToken ct)
    {
        var payment = await sender.Send(new RecordOrganisationPaymentCommand(
            orgId,
            body.AmountGbp,
            body.PaymentDate,
            body.PaymentMethod,
            body.Reference,
            body.Notes), ct);

        return Results.Created(
            $"/v1/organisations/{orgId}/payments/{payment.PaymentId}",
            payment);
    }

}

/// <summary>Response body for <c>GET /v1/organisations</c>.</summary>
public record OrganisationListResponse(IReadOnlyList<OrganisationSummary> Items);
