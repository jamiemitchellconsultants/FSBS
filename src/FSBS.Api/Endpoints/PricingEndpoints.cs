using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.Pricing.Services;
using FSBS.Domain.Enums;

namespace FSBS.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for stateless pricing quote calculation.
/// Routes are under <c>/v1/pricing</c> and require authentication.
/// </summary>
public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/pricing")
            .WithTags("Pricing")
            .RequireAuthorization();

        group.MapGet("/quote", GetQuoteAsync)
            .WithName("GetPricingQuote")
            .WithSummary("Stateless price preview. Called on every booking wizard step change. No DB write.")
            .Produces<PricingQuoteResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetQuoteAsync(
        Guid configurationId,
        TrainingType trainingType,
        int studentCount,
        int durationMins,
        DateTimeOffset slotStart,
        IPricingService pricingService,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (durationMins < 240)
            return Results.Problem(
                detail: "Minimum booking duration is 240 minutes (4 hours).",
                statusCode: StatusCodes.Status400BadRequest);

        if (studentCount < 1)
            return Results.Problem(
                detail: "studentCount must be at least 1.",
                statusCode: StatusCodes.Status400BadRequest);

        // Determine customer class from the caller's role.
        var customerClass = currentUser.Role switch
        {
            AppRole.CorporateManager or AppRole.CorporateStudent => CustomerClass.Corporate,
            AppRole.InternalStudent                              => CustomerClass.Staff,
            _                                                    => CustomerClass.Standard,
        };

        var request = new PricingRequest(
            ConfigurationId: configurationId,
            TrainingType: trainingType,
            CustomerClass: customerClass,
            BookerRole: currentUser.Role,
            DurationMins: durationMins,
            StudentCount: studentCount,
            SlotStart: slotStart,
            OrgId: currentUser.OrgId);

        try
        {
            var result = await pricingService.CalculateAsync(request, ct);

            return Results.Ok(new PricingQuoteResponse(
                GrossPriceGbp: result.GrossPrice.Amount,
                DiscountGbp: result.DiscountAmount.Amount,
                NetPriceGbp: result.NetPrice.Amount,
                AppliedDiscounts: result.AppliedDiscounts
                    .Select(d => new AppliedDiscountDto(
                        d.DiscountRuleId,
                        d.DiscountType.ToString(),
                        d.DiscountPct,
                        d.DiscountAmount.Amount))
                    .ToList()));
        }
        catch (PricingPolicyNotFoundException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }
}

/// <summary>Response body for <c>GET /v1/pricing/quote</c>.</summary>
public record PricingQuoteResponse(
    decimal GrossPriceGbp,
    decimal DiscountGbp,
    decimal NetPriceGbp,
    IReadOnlyList<AppliedDiscountDto> AppliedDiscounts);

/// <summary>A single discount rule applied to the pricing quote.</summary>
public record AppliedDiscountDto(
    Guid DiscountRuleId,
    string DiscountType,
    decimal DiscountPct,
    decimal DiscountAmountGbp);
