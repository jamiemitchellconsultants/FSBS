using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Emails the booker (and optionally the admin) when a booking is cancelled.
/// </summary>
internal sealed class BookingCancelledHandler(
    ISesEmailService ses,
    IUserLookupService users,
    ILogger<BookingCancelledHandler> logger) : INotificationHandler<BookingCancelledEvent>
{
    private const string TemplateName = "FsbsBookingCancelled";

    public async Task HandleAsync(BookingCancelledEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation("Sending BookingCancelled notification for booking {BookingId}.", notification.BookingId);

        var booker = await users.GetContactAsync(notification.BookedBy, ct);
        if (booker is null)
        {
            logger.LogWarning("BookingCancelled: booker {UserId} not found — skipping email.", notification.BookedBy);
            return;
        }

        await ses.SendTemplatedEmailAsync(booker.Email, TemplateName, new
        {
            name             = booker.DisplayName,
            bookingId        = notification.BookingId,
            cancelledByAdmin = notification.CancelledByAdmin,
            reason           = notification.Reason,
            cancelledAt      = notification.OccurredAt
        }, ct);
    }
}
