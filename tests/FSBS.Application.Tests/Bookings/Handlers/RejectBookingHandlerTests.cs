using FSBS.Application.Tests.Common;
using FSBS.Domain.Events;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings.Handlers;

/// <summary>
/// Side-effect coverage for RejectBookingHandler. State-machine guards
/// (non-PendingApproval, self-approval) live in
/// <c>StateMachine/RejectBookingHandlerStateTests</c>.
/// </summary>
[Trait("Category", "Unit")]
public class RejectBookingHandlerTests : HandlerFixtureBase
{
    private RejectBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService);

    [Fact]
    public async Task Handle_RejectsAndEmitsEventWithReason()
    {
        var bookerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(bookerId).WithApproval().Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.SalesStaff, userId: reviewerId);

        await Build().Handle(
            new RejectBookingCommand(booking.Id, "Department lacks budget"),
            CancellationToken.None);

        var evt = booking.DomainEvents.OfType<BookingRejectedEvent>().Single();
        evt.BookingId.Should().Be(booking.Id);
        evt.BookedBy.Should().Be(bookerId);
        evt.ReviewedBy.Should().Be(reviewerId);
        evt.RejectionReason.Should().Be("Department lacks budget");
    }

    [Fact]
    public async Task Handle_CallsReconfigCleanup()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(
            new RejectBookingCommand(booking.Id, "Department lacks budget"),
            CancellationToken.None);

        await ReconfigurationService.Received(1)
            .RemoveOrphanedSlotsAsync(booking, Arg.Any<CancellationToken>());
    }
}
