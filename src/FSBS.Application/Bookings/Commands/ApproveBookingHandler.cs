using FSBS.Application.Bookings.Services;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Application.Pricing.Services;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.Bookings.Commands;

public sealed class ApproveBookingHandler(
    ICurrentUser currentUser,
    IBookingRepository bookingRepository,
    IReconfigurationService reconfigurationService,
    IReconfigurationSlotRepository reconfigSlotRepository,
    IPricingService pricingService)
    : IRequestHandler<ApproveBookingCommand, ApproveBookingResult>
{
    /// <inheritdoc/>
    public async Task<ApproveBookingResult> Handle(
        ApproveBookingCommand command, CancellationToken ct)
    {
        var booking = await bookingRepository.FindByIdAsync(command.BookingId, ct)
            ?? throw new BookingNotFoundException(command.BookingId);

        if (booking.Status != BookingStatus.PendingApproval)
            throw new InvalidBookingStateTransitionException(
                booking.Id, booking.Status, BookingStatus.Confirmed);

        if (booking.BookedBy == currentUser.UserId)
            throw new SelfApprovalException(booking.Id);

        var slot = booking.Slots.First();

        var pricing = await pricingService.CalculateAsync(
            new PricingRequest(
                ConfigurationId: booking.ConfigId,
                TrainingType: booking.TrainingType,
                CustomerClass: DeriveCustomerClass(booking.BookerRole),
                BookerRole: booking.BookerRole,
                DurationMins: slot.DurationMins,
                StudentCount: booking.StudentCount,
                SlotStart: slot.StartAt,
                OrgId: booking.OrgId),
            ct);

        LockPrice(booking, pricing);
        ApproveRecord(booking);

        var reconfigSlot = await reconfigurationService.BuildSlotForConfirmedBookingAsync(
            booking, slot, ct);

        if (reconfigSlot is not null)
            await reconfigSlotRepository.AddAsync(reconfigSlot, ct);

        booking.AddDomainEvent(new BookingApprovedEvent(
            BookingId: booking.Id,
            BookedBy: booking.BookedBy,
            ReviewedBy: currentUser.UserId));

        return new ApproveBookingResult(
            booking.Id,
            booking.GrossPriceGbp!.Value,
            booking.DiscountPct ?? 0,
            booking.NetPriceGbp!.Value);
    }

    private void LockPrice(Booking booking, PricingResult pricing)
    {
        booking.Status = BookingStatus.Confirmed;
        booking.GrossPriceGbp = pricing.GrossPrice.Amount;
        booking.DiscountPct = pricing.GrossPrice.Amount > 0
            ? Math.Round(pricing.DiscountAmount.Amount / pricing.GrossPrice.Amount * 100, 2)
            : 0;
        booking.NetPriceGbp = pricing.NetPrice.Amount;

        foreach (var d in pricing.AppliedDiscounts)
        {
            booking.Discounts.Add(new BookingDiscount
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                DiscountRuleId = d.DiscountRuleId,
                DiscountType = d.DiscountType,
                DiscountPct = d.DiscountPct,
                AmountGbp = d.DiscountAmount.Amount,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
    }

    private void ApproveRecord(Booking booking)
    {
        var approval = booking.Approval
            ?? throw new InvalidOperationException(
                $"Booking {booking.Id} has no approval record.");

        approval.Decision = ApprovalDecision.Approved;
        approval.ReviewedBy = currentUser.UserId;
        approval.ReviewedAt = DateTimeOffset.UtcNow;
    }

    private static CustomerClass DeriveCustomerClass(AppRole role) => role switch
    {
        AppRole.InternalStudent => CustomerClass.Staff,
        AppRole.CorporateManager or AppRole.CorporateStudent => CustomerClass.Corporate,
        _ => CustomerClass.Standard,
    };
}
