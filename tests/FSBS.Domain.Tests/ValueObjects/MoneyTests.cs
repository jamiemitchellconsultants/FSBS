namespace FSBS.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class MoneyTests
{
    [Fact]
    public void DefaultConstructor_DefaultsCurrencyToGbp()
    {
        var amount = new Money(100m);
        amount.Currency.Should().Be("GBP");
    }

    [Fact]
    public void Zero_HasZeroAmountAndGbpCurrency()
    {
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.Currency.Should().Be("GBP");
        Money.Zero.IsZero.Should().BeTrue();
        Money.Zero.IsPositive.Should().BeFalse();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void IsPositive_WhenAmountAboveZero_ReturnsTrue(decimal amount)
    {
        new Money(amount).IsPositive.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0)]
    [InlineData(-1_000_000)]
    public void IsPositive_WhenAmountAtOrBelowZero_ReturnsFalse(decimal amount)
    {
        new Money(amount).IsPositive.Should().BeFalse();
    }

    [Fact]
    public void Addition_WithSameCurrency_SumsAmounts()
    {
        var sum = new Money(40m) + new Money(2.50m);
        sum.Should().Be(new Money(42.50m));
    }

    [Fact]
    public void Subtraction_WithSameCurrency_DifferencesAmounts()
    {
        var diff = new Money(50m) - new Money(7.50m);
        diff.Should().Be(new Money(42.50m));
    }

    [Fact]
    public void Multiplication_ScalesAmountAndPreservesCurrency()
    {
        var scaled = new Money(10m, "USD") * 3m;
        scaled.Should().Be(new Money(30m, "USD"));
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_Throws()
    {
        var act = () => _ = new Money(10m, "GBP") + new Money(10m, "USD");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot combine Money values with different currencies*");
    }

    [Fact]
    public void Subtraction_WithDifferentCurrencies_Throws()
    {
        var act = () => _ = new Money(10m, "GBP") - new Money(10m, "USD");
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(1.005, 1.01)]   // round half away from zero
    [InlineData(1.004, 1.00)]
    [InlineData(-1.005, -1.01)]
    [InlineData(123.4567, 123.46)]
    public void RoundToTwoDecimalPlaces_RoundsUsingAwayFromZero(decimal input, decimal expected)
    {
        new Money(input).RoundToTwoDecimalPlaces().Amount.Should().Be(expected);
    }
}
