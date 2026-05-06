using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Emails the InternalStudent when their booking has been approved by SalesStaff.
/// </summary>
internal sealed class BookingApprovedHandler(
    ISesEmailService ses,
    IUserLookupService users,
    ILogger<BookingApprovedHandler> logger) : INotificationHandler<BookingApprovedEvent>
{
    private const string TemplateName = "FsbsBookingApproved";

    public async Task HandleAsync(BookingApprovedEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation("Sending BookingApproved notification for booking {BookingId}.", notification.BookingId);

        var booker = await users.GetContactAsync(notification.BookedBy, ct);
        if (booker is null)
        {
            logger.LogWarning("BookingApproved: booker {UserId} not found — skipping email.", notification.BookedBy);
            return;
        }

        await ses.SendTemplatedEmailAsync(booker.Email, TemplateName, new
        {
            name      = booker.DisplayName,
            bookingId = notification.BookingId,
            approvedAt = notification.OccurredAt
        }, ct);
    }
}
