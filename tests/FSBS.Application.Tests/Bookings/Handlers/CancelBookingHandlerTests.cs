using FSBS.Application.Tests.Common;
using FSBS.Domain.Events;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings.Handlers;

/// <summary>
/// Side-effect coverage for CancelBookingHandler. State-machine and ownership
/// tests live in <c>StateMachine/CancelBookingHandlerStateTests</c>.
/// </summary>
[Trait("Category", "Unit")]
public class CancelBookingHandlerTests : HandlerFixtureBase
{
    private CancelBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService);

    [Fact]
    public async Task Handle_StaffCancellation_RemovesOrphanedReconfigSlots()
    {
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(BookingStatus.Confirmed)
            .WithBookedBy(Guid.NewGuid())
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "ops"), CancellationToken.None);

        await ReconfigurationService.Received(1)
            .RemoveOrphanedSlotsAsync(booking, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmitsBookingCancelledEvent_WithStaffFlag()
    {
        var bookerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(BookingStatus.Confirmed).WithBookedBy(bookerId).Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.SalesStaff, userId: staffId);

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "Customer no-show"), CancellationToken.None);

        var evt = booking.DomainEvents.OfType<BookingCancelledEvent>().Single();
        evt.BookingId.Should().Be(booking.Id);
        evt.BookedBy.Should().Be(bookerId);
        evt.CancelledBy.Should().Be(staffId);
        evt.CancelledByAdmin.Should().BeTrue();
        evt.Reason.Should().Be("Customer no-show");
    }

    [Fact]
    public async Task Handle_EmitsBookingCancelledEvent_WithoutStaffFlag_WhenOwnerCancels()
    {
        var ownerId = Guid.NewGuid();
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(BookingStatus.Confirmed).WithBookedBy(ownerId).Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.PrivateCustomer, userId: ownerId);

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "Changed plans"), CancellationToken.None);

        var evt = booking.DomainEvents.OfType<BookingCancelledEvent>().Single();
        evt.CancelledByAdmin.Should().BeFalse();
        evt.CancelledBy.Should().Be(ownerId);
    }
}
