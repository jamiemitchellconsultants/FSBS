using System.Text.Json;
using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Interfaces;
using FSBS.Domain.ValueObjects;

namespace FSBS.Application.Pricing.Services;

public sealed class PricingService(IPricingPolicyRepository pricingPolicyRepository) : IPricingService
{
    public async Task<PricingResult> CalculateAsync(PricingRequest request, CancellationToken ct = default)
    {
        var effectiveCustomerClass = request.BookerRole == AppRole.InternalStudent
            ? CustomerClass.Staff
            : request.CustomerClass;

        var policy = await pricingPolicyRepository.FindApplicableAsync(
            request.ConfigurationId,
            request.TrainingType,
            effectiveCustomerClass,
            request.SlotStart,
            ct)
            ?? throw new PricingPolicyNotFoundException(
                request.ConfigurationId, request.TrainingType, effectiveCustomerClass);

        var grossPrice = CalculateGross(policy.HourlyRateGbp, request.DurationMins);

        if (request.BookerRole == AppRole.InternalStudent)
            return new PricingResult(grossPrice, Money.Zero, grossPrice, []);

        var rules = await pricingPolicyRepository.GetDiscountRulesAsync(policy.Id, ct);
        var appliedDiscounts = EvaluateDiscounts(rules, grossPrice, request);

        var totalDiscount = appliedDiscounts
            .Aggregate(Money.Zero, (sum, d) => sum + d.DiscountAmount)
            .RoundToTwoDecimalPlaces();

        var netPrice = (grossPrice - totalDiscount).RoundToTwoDecimalPlaces();

        return new PricingResult(grossPrice, totalDiscount, netPrice, appliedDiscounts);
    }

    private static Money CalculateGross(decimal hourlyRateGbp, int durationMins)
    {
        var hours = durationMins / 60m;
        return new Money(Math.Round(hourlyRateGbp * hours, 2, MidpointRounding.AwayFromZero));
    }

    private static IReadOnlyList<AppliedDiscount> EvaluateDiscounts(
        IReadOnlyList<DiscountRule> rules,
        Money grossPrice,
        PricingRequest request)
    {
        var eligible = rules
            .Where(r => IsEligible(r, request))
            .OrderByDescending(r => r.Priority)
            .ToList();

        if (eligible.Count == 0)
            return [];

        var topRule = eligible[0];

        List<DiscountRule> toApply;

        if (!topRule.IsCombinable)
        {
            toApply = [topRule];
        }
        else
        {
            toApply = eligible.Where(r => r.IsCombinable).ToList();
        }

        return toApply
            .Select(r => ToAppliedDiscount(r, grossPrice))
            .ToList()
            .AsReadOnly();
    }

    private static AppliedDiscount ToAppliedDiscount(DiscountRule rule, Money grossPrice)
    {
        var discountAmount = (grossPrice * (rule.DiscountPct / 100m))
            .RoundToTwoDecimalPlaces();

        return new AppliedDiscount(rule.Id, rule.DiscountType, rule.DiscountPct, discountAmount);
    }

    private static bool IsEligible(DiscountRule rule, PricingRequest request) =>
        rule.DiscountType switch
        {
            DiscountType.AdvanceBooking => CheckAdvanceBooking(rule, request),
            DiscountType.VolumeOrgSession => CheckVolumeOrgSession(rule, request),
            DiscountType.VolumeAdvanceBlock => CheckAdvanceBooking(rule, request),
            DiscountType.CorporateNegotiated => request.CustomerClass == CustomerClass.Corporate,
            DiscountType.Promotional => false,
            DiscountType.StaffRate => false,
            _ => false
        };

    private static bool CheckAdvanceBooking(DiscountRule rule, PricingRequest request)
    {
        if (string.IsNullOrWhiteSpace(rule.ThresholdJson))
            return false;

        var threshold = JsonSerializer.Deserialize<AdvanceBookingThreshold>(
            rule.ThresholdJson,
            JsonOptions);

        if (threshold is null)
            return false;

        var daysAhead = (request.SlotStart.Date - DateTimeOffset.UtcNow.Date).Days;
        return daysAhead >= threshold.MinDaysAhead;
    }

    private static bool CheckVolumeOrgSession(DiscountRule rule, PricingRequest request)
    {
        if (request.OrgId is null || string.IsNullOrWhiteSpace(rule.ThresholdJson))
            return false;

        var threshold = JsonSerializer.Deserialize<VolumeOrgSessionThreshold>(
            rule.ThresholdJson,
            JsonOptions);

        return threshold is not null && request.OrgConfirmedSessionCount >= threshold.MinSessions;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private record AdvanceBookingThreshold(int MinDaysAhead);
    private record VolumeOrgSessionThreshold(int MinSessions);
}
