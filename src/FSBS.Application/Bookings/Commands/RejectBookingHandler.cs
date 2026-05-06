using FSBS.Application.Bookings.Services;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.Bookings.Commands;

public sealed class RejectBookingHandler(
    ICurrentUser currentUser,
    IBookingRepository bookingRepository,
    IReconfigurationService reconfigurationService)
    : IRequestHandler<RejectBookingCommand, RejectBookingResult>
{
    /// <inheritdoc/>
    public async Task<RejectBookingResult> Handle(
        RejectBookingCommand command, CancellationToken ct)
    {
        var booking = await bookingRepository.FindByIdAsync(command.BookingId, ct)
            ?? throw new BookingNotFoundException(command.BookingId);

        if (booking.Status != BookingStatus.PendingApproval)
            throw new InvalidBookingStateTransitionException(
                booking.Id, booking.Status, BookingStatus.Rejected);

        if (booking.BookedBy == currentUser.UserId)
            throw new SelfApprovalException(booking.Id);

        // Release all reserved slots.
        foreach (var slot in booking.Slots)
            slot.SlotStatus = SlotStatus.Cancelled;

        // Transition state.
        booking.Status = BookingStatus.Rejected;

        // Record the decision on the approval record.
        var approval = booking.Approval
            ?? throw new InvalidOperationException(
                $"Booking {booking.Id} has no approval record.");

        approval.Decision = ApprovalDecision.Rejected;
        approval.RejectionReason = command.Reason;
        approval.ReviewedBy = currentUser.UserId;
        approval.ReviewedAt = DateTimeOffset.UtcNow;

        // Remove any reconfig slots that were reserved around this booking
        // and re-evaluate the preceding booking's reconfig slot.
        await reconfigurationService.RemoveOrphanedSlotsAsync(booking, ct);

        booking.AddDomainEvent(new BookingRejectedEvent(
            BookingId: booking.Id,
            BookedBy: booking.BookedBy,
            ReviewedBy: currentUser.UserId,
            RejectionReason: command.Reason));

        return new RejectBookingResult(booking.Id);
    }
}
