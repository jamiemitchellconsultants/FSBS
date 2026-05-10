using FSBS.Application.Organisations.Queries;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using FSBS.Application.Common.Interfaces;
using FSBS.Shared.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        Guid orgId, FsbsDbContext db, CancellationToken ct)
    {
        var account = await db.OrgAccounts
            .Include(a => a.Organisation)
            .Include(a => a.Payments.Where(p => !p.IsDeleted).OrderByDescending(p => p.PaymentDate).Take(20))
            .FirstOrDefaultAsync(a => a.OrgId == orgId, ct);

        if (account is null) return Results.NotFound();

        return Results.Ok(MapAccount(account));
    }

    private static async Task<IResult> RecordPaymentAsync(
        Guid orgId,
        RecordPaymentRequest body,
        FsbsDbContext db,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (body.AmountGbp <= 0)
            return Results.Problem(
                detail: "Amount must be greater than zero.",
                statusCode: StatusCodes.Status400BadRequest);

        if (!new[] { "BankTransfer", "Cheque", "Cash", "CreditNote", "Adjustment" }.Contains(body.PaymentMethod))
            return Results.Problem(
                detail: $"Invalid payment method '{body.PaymentMethod}'.",
                statusCode: StatusCodes.Status400BadRequest);

        var account = await db.OrgAccounts
            .FirstOrDefaultAsync(a => a.OrgId == orgId, ct);

        if (account is null) return Results.NotFound();

        var payment = new AccountPayment
        {
            Id = Guid.NewGuid(),
            OrgAccountId = account.Id,
            OrgId = orgId,
            AmountGbp = body.AmountGbp,
            PaymentDate = body.PaymentDate,
            RecordedBy = currentUser.UserId,
            PaymentMethod = body.PaymentMethod,
            Status = PaymentStatus.Pending,
            Reference = body.Reference?.Trim(),
            Notes = body.Notes?.Trim(),
        };

        db.AccountPayments.Add(payment);
        await db.SaveChangesAsync(ct);

        return Results.Created(
            $"/v1/organisations/{orgId}/payments/{payment.Id}",
            MapPayment(payment));
    }

    private static OrgAccountDto MapAccount(OrgAccount a) =>
        new(
            a.Id,
            a.OrgId,
            a.Organisation.Name,
            a.CreditLimitGbp,
            a.CurrentBalanceGbp,
            a.Status.ToString(),
            a.PaymentTermsDays,
            a.Payments.Select(MapPayment).ToList());

    private static PaymentDto MapPayment(AccountPayment p) =>
        new(
            p.Id,
            p.OrgId,
            p.AmountGbp,
            p.PaymentDate,
            p.PaymentMethod.ToString(),
            p.Status.ToString(),
            p.Reference,
            p.Notes,
            p.CreatedAt);
}

/// <summary>Response body for <c>GET /v1/organisations</c>.</summary>
public record OrganisationListResponse(IReadOnlyList<OrganisationSummary> Items);
