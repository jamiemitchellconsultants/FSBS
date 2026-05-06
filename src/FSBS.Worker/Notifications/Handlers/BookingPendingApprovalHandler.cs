using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;
using Microsoft.Extensions.Options;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Notifies SalesStaff / SystemAdmin that an InternalStudent booking is
/// awaiting approval. Triggered by <see cref="SlotBookedEvent"/> when the
/// booker role is InternalStudent (booking enters PendingApproval state).
/// </summary>
internal sealed class BookingPendingApprovalHandler(
    ISesEmailService ses,
    IUserLookupService users,
    IOptions<WorkerSettings> options,
    ILogger<BookingPendingApprovalHandler> logger) : INotificationHandler<SlotBookedEvent>
{
    private const string TemplateName = "FsbsBookingPendingApproval";
    private readonly string _salesStaffEmail = options.Value.SalesStaffEmail;

    public async Task HandleAsync(SlotBookedEvent notification, CancellationToken ct = default)
    {
        // Only act on InternalStudent bookings — others go straight to Provisional.
        if (notification.BookerRole != FSBS.Domain.Enums.AppRole.InternalStudent)
            return;

        logger.LogInformation(
            "Sending BookingPendingApproval notification for booking {BookingId}.",
            notification.BookingId);

        var booker = await users.GetContactAsync(notification.BookedBy, ct);

        await ses.SendTemplatedEmailAsync(
            toAddress: _salesStaffEmail,
            templateName: TemplateName,
            templateData: new
            {
                bookingId   = notification.BookingId,
                bookerName  = booker?.DisplayName ?? notification.BookedBy.ToString(),
                trainingType = notification.TrainingType.ToString(),
                slotStart   = notification.SlotStart,
                slotEnd     = notification.SlotEnd,
                studentCount = notification.StudentCount
            }, ct);
    }
}
