namespace FSBS.Domain.ValueObjects;

/// <summary>
/// An amount of money in a specific currency. Immutable.
/// All monetary values in FSBS are GBP; the currency field exists to make
/// that assumption explicit and catchable if it ever changes.
/// </summary>
public record Money(decimal Amount, string Currency = "GBP")
{
    public static readonly Money Zero = new(0m);

    /// <summary>Adds two Money values. Both must share the same currency.</summary>
    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    /// <summary>Subtracts <paramref name="b"/> from <paramref name="a"/>. Both must share the same currency.</summary>
    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    /// <summary>Multiplies a Money value by a scalar factor.</summary>
    public static Money operator *(Money money, decimal factor) =>
        new(money.Amount * factor, money.Currency);

    /// <summary>Returns true when <see cref="Amount"/> is greater than zero.</summary>
    public bool IsPositive => Amount > 0m;

    /// <summary>Returns true when <see cref="Amount"/> is exactly zero.</summary>
    public bool IsZero => Amount == 0m;

    /// <summary>
    /// Returns a new Money with <see cref="Amount"/> rounded to 2 decimal places
    /// using <see cref="MidpointRounding.AwayFromZero"/>.
    /// </summary>
    public Money RoundToTwoDecimalPlaces() =>
        this with { Amount = Math.Round(Amount, 2, MidpointRounding.AwayFromZero) };

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException(
                $"Cannot combine Money values with different currencies: {a.Currency} vs {b.Currency}.");
    }
}
