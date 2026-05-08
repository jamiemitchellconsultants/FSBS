using FSBS.Application.Pricing.Services;
using FSBS.Application.Tests.Common;
using FSBS.Domain.Entities;
using FSBS.Domain.Events;
using FSBS.Domain.Tests.Builders;
using FSBS.Domain.ValueObjects;

namespace FSBS.Application.Tests.Bookings.Handlers;

/// <summary>
/// Side-effect coverage for ApproveBookingHandler. State-machine guards
/// (non-PendingApproval rejection, self-approval ban) are covered in
/// <c>StateMachine/ApproveBookingHandlerStateTests</c>.
/// </summary>
[Trait("Category", "Unit")]
public class ApproveBookingHandlerTests : HandlerFixtureBase
{
    private ApproveBookingHandler Build() => new(
        CurrentUser,
        BookingRepository,
        ReconfigurationService,
        ReconfigurationSlotRepository,
        PricingService);

    private static PricingResult Pricing(decimal gross, decimal discount, decimal net,
        params AppliedDiscount[] applied) =>
        new(new Money(gross), new Money(discount), new Money(net), applied);

    private void StubBooking(Booking booking) =>
        BookingRepository.FindByIdAsync(booking.Id, Arg.Any<CancellationToken>())
            .Returns(booking);

    private void StubPricing(PricingResult pricing) =>
        PricingService.CalculateAsync(Arg.Any<PricingRequest>(), Arg.Any<CancellationToken>())
            .Returns(pricing);

    // ── Reconfiguration slot insertion ───────────────────────────────────────

    [Fact]
    public async Task Handle_WhenReconfigSlotReturned_InsertsItOnce()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        StubBooking(booking);
        StubPricing(Pricing(0m, 0m, 0m));
        CurrentUser = FakeCurrentUser.SalesStaff();

        var reconfig = new ReconfigurationSlot { Id = Guid.NewGuid() };
        ReconfigurationService
            .BuildSlotForConfirmedBookingAsync(booking, Arg.Any<BookingSlot>(), Arg.Any<CancellationToken>())
            .Returns(reconfig);

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        await ReconfigurationSlotRepository.Received(1)
            .AddAsync(reconfig, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReconfigSlotIsNull_DoesNotCallAdd()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        StubBooking(booking);
        StubPricing(Pricing(0m, 0m, 0m));
        CurrentUser = FakeCurrentUser.SalesStaff();

        ReconfigurationService
            .BuildSlotForConfirmedBookingAsync(booking, Arg.Any<BookingSlot>(), Arg.Any<CancellationToken>())
            .Returns((ReconfigurationSlot?)null);

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        await ReconfigurationSlotRepository.DidNotReceive()
            .AddAsync(Arg.Any<ReconfigurationSlot>(), Arg.Any<CancellationToken>());
    }

    // ── Pricing snapshot ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LocksPriceFromPricingResultAtConfirmation()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        StubBooking(booking);
        // Gross 1200, discount 240 → DiscountPct = 20%, NetPrice = 960
        StubPricing(Pricing(1200m, 240m, 960m));
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        booking.GrossPriceGbp.Should().Be(1200m);
        booking.NetPriceGbp.Should().Be(960m);
        booking.DiscountPct.Should().Be(20m);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_ZeroGrossPrice_StoresDiscountPctAsZero()
    {
        // Avoids division-by-zero when computing the percentage from absolute amounts.
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        StubBooking(booking);
        StubPricing(Pricing(0m, 0m, 0m));
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        booking.DiscountPct.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_AppliedDiscounts_AreSnapshotIntoBookingDiscounts()
    {
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(Guid.NewGuid()).WithApproval().Build();
        StubBooking(booking);
        var ruleId = Guid.NewGuid();
        StubPricing(Pricing(1000m, 100m, 900m,
            new AppliedDiscount(ruleId, DiscountType.VolumeAdvanceBlock, 10m, new Money(100m))));
        CurrentUser = FakeCurrentUser.SalesStaff();

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        booking.Discounts.Should().ContainSingle();
        var d = booking.Discounts.Single();
        d.DiscountRuleId.Should().Be(ruleId);
        d.DiscountType.Should().Be(DiscountType.VolumeAdvanceBlock);
        d.DiscountPct.Should().Be(10m);
        d.AmountGbp.Should().Be(100m);
    }

    // ── Event emission ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmitsBookingApprovedEventOnAggregate()
    {
        var bookerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var booking = BookingBuilder.ForInternalStudent()
            .WithBookedBy(bookerId).WithApproval().Build();
        StubBooking(booking);
        StubPricing(Pricing(0m, 0m, 0m));
        CurrentUser = new FakeCurrentUser(AppRole.SalesStaff, userId: reviewerId);

        await Build().Handle(new ApproveBookingCommand(booking.Id), CancellationToken.None);

        var evt = booking.DomainEvents.OfType<BookingApprovedEvent>().Single();
        evt.BookingId.Should().Be(booking.Id);
        evt.BookedBy.Should().Be(bookerId);
        evt.ReviewedBy.Should().Be(reviewerId);
    }
}
