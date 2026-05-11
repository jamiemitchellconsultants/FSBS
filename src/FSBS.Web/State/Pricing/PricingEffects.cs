using Fluxor;
using FSBS.Web.Services;

namespace FSBS.Web.State.Pricing;

public sealed class PricingEffects(PricingService pricingSvc)
{
    [EffectMethod]
    public async Task HandleFetchQuote(FetchPriceQuoteAction action, IDispatcher dispatcher)
    {
        dispatcher.Dispatch(new SetPriceLoadingAction(true));
        try
        {
            var quote = await pricingSvc.GetQuoteAsync(
                action.ConfigurationId,
                action.TrainingType,
                action.CustomerClass,
                action.DurationMins,
                action.StudentCount,
                action.SlotStart,
                action.OrgId);

            if (quote is null)
            {
                dispatcher.Dispatch(new SetPriceErrorAction("No pricing policy found for the selected configuration."));
                return;
            }

            dispatcher.Dispatch(new SetPriceQuoteAction(
                quote.GrossPriceGbp,
                quote.DiscountPct,
                quote.NetPriceGbp,
                action.CustomerClass));
        }
        catch
        {
            dispatcher.Dispatch(new SetPriceErrorAction("Failed to fetch price quote. Please try again."));
        }
    }
}
