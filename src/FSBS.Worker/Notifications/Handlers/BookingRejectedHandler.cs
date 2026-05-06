using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Emails the InternalStudent when their booking has been rejected, including
/// the mandatory rejection reason.
/// </summary>
internal sealed class BookingRejectedHandler(
    ISesEmailService ses,
    IUserLookupService users,
    ILogger<BookingRejectedHandler> logger) : INotificationHandler<BookingRejectedEvent>
{
    private const string TemplateName = "FsbsBookingRejected";

    public async Task HandleAsync(BookingRejectedEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation("Sending BookingRejected notification for booking {BookingId}.", notification.BookingId);

        var booker = await users.GetContactAsync(notification.BookedBy, ct);
        if (booker is null)
        {
            logger.LogWarning("BookingRejected: booker {UserId} not found — skipping email.", notification.BookedBy);
            return;
        }

        await ses.SendTemplatedEmailAsync(booker.Email, TemplateName, new
        {
            name            = booker.DisplayName,
            bookingId       = notification.BookingId,
            rejectionReason = notification.RejectionReason,
            rejectedAt      = notification.OccurredAt
        }, ct);
    }
}
