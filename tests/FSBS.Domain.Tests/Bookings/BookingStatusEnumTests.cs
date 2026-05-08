namespace FSBS.Domain.Tests.Bookings;

/// <summary>
/// Locks down the set of <see cref="BookingStatus"/> values. The state machine
/// in CLAUDE.md depends on these names existing exactly as written; renaming or
/// removing one is a breaking change for handlers, DB enum, and the API DTOs.
/// </summary>
[Trait("Category", "Unit")]
public class BookingStatusEnumTests
{
    [Theory]
    [InlineData(nameof(BookingStatus.Provisional))]
    [InlineData(nameof(BookingStatus.PendingApproval))]
    [InlineData(nameof(BookingStatus.Confirmed))]
    [InlineData(nameof(BookingStatus.InProgress))]
    [InlineData(nameof(BookingStatus.Completed))]
    [InlineData(nameof(BookingStatus.Invoiced))]
    [InlineData(nameof(BookingStatus.CancelledByCustomer))]
    [InlineData(nameof(BookingStatus.CancelledByAdmin))]
    [InlineData(nameof(BookingStatus.Rejected))]
    [InlineData(nameof(BookingStatus.Expired))]
    [InlineData(nameof(BookingStatus.OnHold))]
    public void RequiredStatus_IsDefined(string statusName)
    {
        Enum.IsDefined(typeof(BookingStatus), statusName).Should().BeTrue(
            $"BookingStatus.{statusName} is referenced by handlers and the DB enum");
    }

    [Fact]
    public void Status_TotalCountIsExactly11()
    {
        // If this fails, a status was added or removed; update the state machine
        // table in CLAUDE.md and the DB enum, then the test list above.
        Enum.GetValues<BookingStatus>().Should().HaveCount(11);
    }
}
