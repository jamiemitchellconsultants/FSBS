namespace FSBS.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class DateTimeRangeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WhenEndIsAfterStart_Succeeds()
    {
        var range = new DateTimeRange(T0, T0.AddHours(4));
        range.DurationMins.Should().Be(240);
    }

    [Fact]
    public void Constructor_WhenEndEqualsStart_Throws()
    {
        var act = () => _ = new DateTimeRange(T0, T0);
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("end");
    }

    [Fact]
    public void Constructor_WhenEndIsBeforeStart_Throws()
    {
        var act = () => _ = new DateTimeRange(T0, T0.AddMinutes(-1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Overlaps_AdjacentRanges_DoNotOverlap()
    {
        // Half-open [Start, End): two adjacent windows touching at the boundary do NOT overlap.
        var first  = new DateTimeRange(T0,            T0.AddHours(4));
        var second = new DateTimeRange(T0.AddHours(4), T0.AddHours(8));
        first.Overlaps(second).Should().BeFalse();
        second.Overlaps(first).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_PartiallyOverlappingRanges_ReturnsTrue()
    {
        var first  = new DateTimeRange(T0,                  T0.AddHours(4));
        var second = new DateTimeRange(T0.AddHours(3),      T0.AddHours(7));
        first.Overlaps(second).Should().BeTrue();
        second.Overlaps(first).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_OneRangeContainsAnother_ReturnsTrue()
    {
        var outer = new DateTimeRange(T0,             T0.AddHours(8));
        var inner = new DateTimeRange(T0.AddHours(2), T0.AddHours(4));
        outer.Overlaps(inner).Should().BeTrue();
        inner.Overlaps(outer).Should().BeTrue();
    }

    [Fact]
    public void ContainsPoint_StartInclusiveEndExclusive()
    {
        var range = new DateTimeRange(T0, T0.AddHours(4));
        range.Contains(T0).Should().BeTrue();                    // inclusive start
        range.Contains(T0.AddHours(2)).Should().BeTrue();
        range.Contains(T0.AddHours(4)).Should().BeFalse();       // exclusive end
        range.Contains(T0.AddMinutes(-1)).Should().BeFalse();
    }

    [Fact]
    public void ContainsRange_WhenInnerFitsExactly_ReturnsTrue()
    {
        var outer = new DateTimeRange(T0, T0.AddHours(8));
        outer.Contains(new DateTimeRange(T0, T0.AddHours(8))).Should().BeTrue();
        outer.Contains(new DateTimeRange(T0.AddHours(1), T0.AddHours(7))).Should().BeTrue();
    }

    [Fact]
    public void ContainsRange_WhenInnerExceedsBoundary_ReturnsFalse()
    {
        var outer = new DateTimeRange(T0, T0.AddHours(8));
        outer.Contains(new DateTimeRange(T0.AddMinutes(-1), T0.AddHours(8))).Should().BeFalse();
        outer.Contains(new DateTimeRange(T0, T0.AddHours(9))).Should().BeFalse();
    }
}
