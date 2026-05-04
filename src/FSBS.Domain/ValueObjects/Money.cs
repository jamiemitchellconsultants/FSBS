namespace FSBS.Domain.ValueObjects;

/// <summary>
/// An amount of money in a specific currency. Immutable.
/// All monetary values in FSBS are GBP; the currency field exists to make
/// that assumption explicit and catchable if it ever changes.
/// </summary>
public record Money(decimal Amount, string Currency = "GBP")
{
    public static readonly Money Zero = new(0m);

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money money, decimal factor) =>
        new(money.Amount * factor, money.Currency);

    public bool IsPositive => Amount > 0m;
    public bool IsZero => Amount == 0m;

    public Money RoundToTwoDecimalPlaces() =>
        this with { Amount = Math.Round(Amount, 2, MidpointRounding.AwayFromZero) };

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot combine Money values with different currencies: {a.Currency} vs {b.Currency}.");
    }
}
