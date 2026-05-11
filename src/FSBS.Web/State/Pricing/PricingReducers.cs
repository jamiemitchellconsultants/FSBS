using Fluxor;

namespace FSBS.Web.State.Pricing;

public static class PricingReducers
{
    [ReducerMethod]
    public static PricingState OnLoading(PricingState state, SetPriceLoadingAction a) =>
        state with { IsLoading = a.IsLoading, Error = null };

    [ReducerMethod]
    public static PricingState OnQuoteReceived(PricingState state, SetPriceQuoteAction a) =>
        state with
        {
            IsLoading     = false,
            GrossPriceGbp = a.GrossPriceGbp,
            DiscountPct   = a.DiscountPct,
            NetPriceGbp   = a.NetPriceGbp,
            CustomerClass = a.CustomerClass,
            Error         = null
        };

    [ReducerMethod]
    public static PricingState OnError(PricingState state, SetPriceErrorAction a) =>
        state with { IsLoading = false, Error = a.Error };

    [ReducerMethod(typeof(ClearPriceQuoteAction))]
    public static PricingState OnClear(PricingState _) => new();
}
