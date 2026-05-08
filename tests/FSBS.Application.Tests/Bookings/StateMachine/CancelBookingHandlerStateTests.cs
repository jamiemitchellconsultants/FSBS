using FSBS.Application.Common.Exceptions;
using FSBS.Application.Tests.Common;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Tests.Builders;

namespace FSBS.Application.Tests.Bookings.StateMachine;

[Trait("Category", "Unit")]
public class CancelBookingHandlerStateTests : HandlerFixtureBase
{
    private CancelBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService);

    [Fact]
    public async Task Handle_BookingNotFound_Throws()
    {
        CurrentUser = FakeCurrentUser.SalesStaff();

        var act = async () => await Build().Handle(
            new CancelBookingCommand(Guid.NewGuid(), "Customer requested"),
            CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Invoiced)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.CancelledByCustomer)]
    [InlineData(BookingStatus.CancelledByAdmin)]
    [InlineData(BookingStatus.Expired)]
    public async Task Handle_TerminalStatus_RefusesToCancel(BookingStatus status)
    {
        var booking = BookingBuilder.ForExternalCustomer().WithStatus(status).Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        var act = async () => await Build().Handle(
            new CancelBookingCommand(booking.Id, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidBookingStateTransitionException>();
    }

    [Theory]
    [InlineData(BookingStatus.Provisional)]
    [InlineData(BookingStatus.PendingApproval)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.OnHold)]
    public async Task Handle_NonTerminalStatusByStaff_TransitionsToCancelledByAdmin(BookingStatus status)
    {
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(status)
            .WithBookedBy(Guid.NewGuid())
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "Customer requested"),
            CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CancelledByAdmin);
        booking.Slots.Should().OnlyContain(s => s.SlotStatus == SlotStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_OwnerCancelsTheirOwn_TransitionsToCancelledByCustomer()
    {
        var ownerId = Guid.NewGuid();
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(BookingStatus.Confirmed)
            .WithBookedBy(ownerId)
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.PrivateCustomer, userId: ownerId);

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "Changed plans"),
            CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CancelledByCustomer);
    }

    [Fact]
    public async Task Handle_NonOwnerCustomer_GetsNotFound()
    {
        // Surfaces a 404 rather than 403 so the API does not leak booking existence
        // to unrelated customers.
        var booking = BookingBuilder.ForExternalCustomer()
            .WithStatus(BookingStatus.Confirmed)
            .WithBookedBy(Guid.NewGuid())
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = new FakeCurrentUser(AppRole.PrivateCustomer);

        var act = async () => await Build().Handle(
            new CancelBookingCommand(booking.Id, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }

    [Fact]
    public async Task Handle_CorporateManagerCancelsOwnOrgBooking_Allowed()
    {
        var orgId = Guid.NewGuid();
        var booking = BookingBuilder.ForCorporateManager(orgId)
            .WithStatus(BookingStatus.Confirmed)
            .WithBookedBy(Guid.NewGuid())   // booked by someone else in the org
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.CorporateManager(orgId);

        await Build().Handle(
            new CancelBookingCommand(booking.Id, "Org-level cancellation"),
            CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CancelledByCustomer);
    }

    [Fact]
    public async Task Handle_CorporateManagerOfDifferentOrg_GetsNotFound()
    {
        var bookingOrg = Guid.NewGuid();
        var attackerOrg = Guid.NewGuid();
        var booking = BookingBuilder.ForCorporateManager(bookingOrg)
            .WithStatus(BookingStatus.Confirmed)
            .WithBookedBy(Guid.NewGuid())
            .Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.CorporateManager(attackerOrg);

        var act = async () => await Build().Handle(
            new CancelBookingCommand(booking.Id, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }
}
