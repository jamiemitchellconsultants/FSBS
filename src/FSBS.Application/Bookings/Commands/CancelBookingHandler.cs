using FSBS.Application.Bookings.Services;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Domain.Exceptions;
using FSBS.Domain.Interfaces;
using MediatR;

namespace FSBS.Application.Bookings.Commands;

public sealed class CancelBookingHandler(
    ICurrentUser currentUser,
    IBookingRepository bookingRepository,
    IReconfigurationService reconfigurationService)
    : IRequestHandler<CancelBookingCommand, CancelBookingResult>
{
    // Terminal states — a booking in any of these cannot be cancelled.
    private static readonly IReadOnlySet<BookingStatus> NonCancellableStatuses =
        new HashSet<BookingStatus>
        {
            BookingStatus.Completed,
            BookingStatus.Invoiced,
            BookingStatus.Rejected,
            BookingStatus.CancelledByCustomer,
            BookingStatus.CancelledByAdmin,
            BookingStatus.Expired,
        };

    /// <inheritdoc/>
    public async Task<CancelBookingResult> Handle(
        CancelBookingCommand command, CancellationToken ct)
    {
        var booking = await bookingRepository.FindByIdAsync(command.BookingId, ct)
            ?? throw new BookingNotFoundException(command.BookingId);

        if (NonCancellableStatuses.Contains(booking.Status))
            throw new InvalidBookingStateTransitionException(
                booking.Id, booking.Status, BookingStatus.CancelledByAdmin);

        var cancelledByAdmin = IsStaffRole(currentUser.Role);

        if (!cancelledByAdmin && !IsBookingOwner(booking))
            throw new BookingNotFoundException(command.BookingId);

        var targetStatus = cancelledByAdmin
            ? BookingStatus.CancelledByAdmin
            : BookingStatus.CancelledByCustomer;

        foreach (var slot in booking.Slots)
            slot.SlotStatus = SlotStatus.Cancelled;

        booking.Status = targetStatus;

        await reconfigurationService.RemoveOrphanedSlotsAsync(booking, ct);

        booking.AddDomainEvent(new BookingCancelledEvent(
            BookingId: booking.Id,
            BookedBy: booking.BookedBy,
            CancelledBy: currentUser.UserId,
            CancelledByAdmin: cancelledByAdmin,
            Reason: command.Reason));

        return new CancelBookingResult(booking.Id, targetStatus);
    }

    // A customer is the owner if they booked it directly, or if they are a
    // CorporateManager acting for the same organisation.
    private bool IsBookingOwner(Domain.Entities.Booking booking) =>
        booking.BookedBy == currentUser.UserId ||
        (currentUser.Role == AppRole.CorporateManager &&
         currentUser.OrgId.HasValue &&
         booking.OrgId == currentUser.OrgId);

    private static bool IsStaffRole(AppRole role) => role is
        AppRole.SystemAdmin or
        AppRole.ScheduleAdmin or
        AppRole.SalesStaff or
        AppRole.CourseDirector or
        AppRole.Instructor or
        AppRole.Management;
}
