using FSBS.Domain.Enums;
using FSBS.Domain.ValueObjects;

namespace FSBS.Application.Pricing.Services;

/// <summary>
/// A single discount rule that was evaluated as eligible and contributed to
/// the final price. One AppliedDiscount maps to one BookingDiscount snapshot
/// written at confirmation.
/// </summary>
public record AppliedDiscount(
    Guid DiscountRuleId,
    string DiscountType,
    decimal DiscountPct,
    Money DiscountAmount);
