using FSBS.Domain.ValueObjects;

namespace FSBS.Application.Pricing.Services;

/// <summary>
/// The fully calculated price for a booking, ready to be locked at confirmation.
/// GrossPrice − DiscountAmount = NetPrice.
/// </summary>
public record PricingResult(
    Money GrossPrice,
    Money DiscountAmount,
    Money NetPrice,
    IReadOnlyList<AppliedDiscount> AppliedDiscounts);
