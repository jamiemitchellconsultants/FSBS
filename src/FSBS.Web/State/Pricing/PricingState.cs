using Fluxor;

namespace FSBS.Web.State.Pricing;

[FeatureState]
public record PricingState
{
    public bool IsLoading { get; init; }
    public decimal? GrossPriceGbp { get; init; }
    public decimal? DiscountPct { get; init; }
    public decimal? NetPriceGbp { get; init; }
    public string? CustomerClass { get; init; }
    public string? Error { get; init; }
}
