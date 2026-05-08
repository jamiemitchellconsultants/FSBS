using FSBS.Domain.Tests.Builders;

namespace FSBS.Domain.Tests.Bookings;

/// <summary>
/// Smoke tests for the test infrastructure itself. Real domain test classes
/// (BookingStateMachineTests, BookingCapacityValidatorTests, etc.) live alongside.
/// </summary>
[Trait("Category", "Unit")]
public class BookingBuilderSmokeTests
{
    [Fact]
    public void ForExternalCustomer_ProducesProvisionalBookingWithFourHourSlot()
    {
        var booking = BookingBuilder.ForExternalCustomer().Build();

        booking.Status.Should().Be(BookingStatus.Provisional);
        booking.Slots.Should().ContainSingle();
        booking.Slots.Single().DurationMins.Should().Be(240);
        booking.DepartmentName.Should().BeNull();
        booking.BudgetCode.Should().BeNull();
    }

    [Fact]
    public void ForInternalStudent_PopulatesDeptAndBudgetAndPendingApproval()
    {
        var booking = BookingBuilder.ForInternalStudent().WithApproval().Build();

        booking.BookerRole.Should().Be(AppRole.InternalStudent);
        booking.Status.Should().Be(BookingStatus.PendingApproval);
        booking.DepartmentName.Should().NotBeNullOrWhiteSpace();
        booking.BudgetCode.Should().NotBeNullOrWhiteSpace();
        booking.Approval.Should().NotBeNull();
        booking.Approval!.Decision.Should().Be(ApprovalDecision.Pending);
    }
}
