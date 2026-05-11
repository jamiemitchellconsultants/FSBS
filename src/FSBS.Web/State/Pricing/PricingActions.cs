namespace FSBS.Web.State.Pricing;

public record FetchPriceQuoteAction(
    Guid ConfigurationId,
    string TrainingType,
    string CustomerClass,
    int DurationMins,
    int StudentCount,
    DateTimeOffset SlotStart,
    Guid? OrgId);

public record SetPriceQuoteAction(decimal GrossPriceGbp, decimal DiscountPct, decimal NetPriceGbp, string CustomerClass);
public record SetPriceLoadingAction(bool IsLoading);
public record SetPriceErrorAction(string? Error);
public record ClearPriceQuoteAction;
