namespace FSBS.Application.Pricing.Services;

public interface IPricingService
{
    /// <summary>
    /// Calculates the gross price, applicable discounts, and net price for a
    /// booking at the point of confirmation. The result is locked immediately
    /// after this call — never recalculated.
    /// </summary>
    Task<PricingResult> CalculateAsync(PricingRequest request, CancellationToken ct = default);
}
