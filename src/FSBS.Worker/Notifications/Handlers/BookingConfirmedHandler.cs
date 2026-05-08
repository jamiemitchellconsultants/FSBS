using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Sends a booking confirmation email to the booker when a booking transitions
/// to Confirmed and the price is locked.
/// </summary>
internal sealed class BookingConfirmedHandler(
    ISesEmailService ses,
    IUserLookupService users,
    ILogger<BookingConfirmedHandler> logger) : INotificationHandler<BookingConfirmedEvent>
{
    private const string TemplateName = "FsbsBookingConfirmed";

    public async Task HandleAsync(BookingConfirmedEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation("Sending BookingConfirmed notification for booking {BookingId}.", notification.BookingId);

        var booker = await users.GetContactAsync(notification.BookedBy, ct);
        if (booker is null)
        {
            logger.LogWarning("BookingConfirmed: booker {UserId} not found — skipping email.", notification.BookedBy);
            return;
        }

        await ses.SendTemplatedEmailAsync(booker.Email, TemplateName, new
        {
            name         = booker.DisplayName,
            bookingId    = notification.BookingId,
            grossPrice   = notification.GrossPriceGbp,
            discount     = notification.DiscountPct,
            netPrice     = notification.NetPriceGbp,
            occurredAt   = notification.OccurredAt
        }, ct);
    }
}
