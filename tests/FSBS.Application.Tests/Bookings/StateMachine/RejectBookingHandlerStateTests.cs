using FSBS.Application.Common.Exceptions;
using FSBS.Application.Tests.Common;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings.StateMachine;

[Trait("Category", "Unit")]
public class RejectBookingHandlerStateTests : HandlerFixtureBase
{
    private RejectBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService);

    [Fact]
    public async Task Handle_BookingNotFound_Throws()
    {
        CurrentUser = FakeCurrentUser.SalesStaff();

        var act = async () => await Build().Handle(
            new RejectBookingCommand(Guid.NewGuid(), "Insufficient justification"),
            CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }

    [Theory]
    [InlineData(BookingStatus.Provisional)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Completed)]
    public async Task Handle_NonPendingApprovalStatus_ThrowsInvalidTransition(BookingStatus status)
    {
        var booking = BookingBuilder.ForInternalStudent().WithStatus(status).WithApproval().Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        var act = async () => await Build().Handle(
            new RejectBookingCommand(booking.Id, "Insufficient justification"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidBookingStateTransitionException>();
    }

    [Fact]
    public async Task Handle_ReviewerEqualsBooker_ThrowsSelfApproval()
    {
        var bookerId = Guid.NewGuid();
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(bookerId)
            .WithApproval(requestedBy: bookerId)
            .Build();

        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.SalesStaff, userId: bookerId);

        var act = async () => await Build().Handle(
            new RejectBookingCommand(booking.Id, "Insufficient justification"),
            CancellationToken.None);

        await act.Should().ThrowAsync<SelfApprovalException>();
    }

    [Fact]
    public async Task Handle_ValidRejection_TransitionsAndCancelsAllSlots()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid())
            .WithApproval()
            .Build();

        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(
            new RejectBookingCommand(booking.Id, "Department lacks budget"),
            CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.Slots.Should().OnlyContain(s => s.SlotStatus == SlotStatus.Cancelled);
        booking.Approval!.Decision.Should().Be(ApprovalDecision.Rejected);
        booking.Approval.RejectionReason.Should().Be("Department lacks budget");
        booking.Approval.ReviewedBy.Should().Be(CurrentUser.UserId);
    }

    [Fact]
    public async Task Handle_ValidRejection_RemovesOrphanedReconfigSlots()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid())
            .WithApproval()
            .Build();

        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(
            new RejectBookingCommand(booking.Id, "Department lacks budget"),
            CancellationToken.None);

        await ReconfigurationService.Received(1).RemoveOrphanedSlotsAsync(
            booking, Arg.Any<CancellationToken>());
    }
}
