using FSBS.Application.Common.Exceptions;
using FSBS.Application.Pricing.Services;
using FSBS.Application.Tests.Common;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Tests.Builders;
using FSBS.Domain.ValueObjects;

namespace FSBS.Application.Tests.Bookings.StateMachine;

[Trait("Category", "Unit")]
public class ApproveBookingHandlerStateTests : HandlerFixtureBase
{
    private ApproveBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService,
        ReconfigurationSlotRepository,
        PricingService);

    private static PricingResult ZeroPricing() => new(
        Money.Zero, Money.Zero, Money.Zero, Array.Empty<AppliedDiscount>());

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsBookingNotFound()
    {
        CurrentUser = FakeCurrentUser.SalesStaff();
        var sut = Build();

        var act = async () => await sut.Handle(
            new ApproveBookingCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<BookingNotFoundException>();
    }

    [Theory]
    [InlineData(BookingStatus.Provisional)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Invoiced)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.CancelledByCustomer)]
    [InlineData(BookingStatus.CancelledByAdmin)]
    [InlineData(BookingStatus.Expired)]
    [InlineData(BookingStatus.OnHold)]
    public async Task Handle_NonPendingApprovalStatus_ThrowsInvalidTransition(BookingStatus status)
    {
        var booking = BookingBuilder.ForInternalStudent().WithStatus(status).WithApproval().Build();
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();

        var act = async () => await Build().Handle(
            new ApproveBookingCommand(booking.Id), CancellationToken.None);

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
        // Same user attempts to approve their own booking
        CurrentUser = new FakeCurrentUser(AppRole.SalesStaff, userId: bookerId);
        PricingService.CalculateAsync(Arg.Any<PricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ZeroPricing());

        var act = async () => await Build().Handle(
            new ApproveBookingCommand(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<SelfApprovalException>();
    }

    [Fact]
    public async Task Handle_ValidApproval_TransitionsToConfirmedAndLocksPrice()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid())
            .WithApproval()
            .Build();

        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);
        CurrentUser = FakeCurrentUser.SalesStaff();
        PricingService.CalculateAsync(Arg.Any<PricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PricingResult(
                new Money(1000m), new Money(0m), new Money(1000m),
                Array.Empty<AppliedDiscount>()));

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.GrossPriceGbp.Should().Be(1000m);
        booking.NetPriceGbp.Should().Be(1000m);
        booking.Approval!.Decision.Should().Be(ApprovalDecision.Approved);
        booking.Approval.ReviewedBy.Should().Be(CurrentUser.UserId);
    }
}
