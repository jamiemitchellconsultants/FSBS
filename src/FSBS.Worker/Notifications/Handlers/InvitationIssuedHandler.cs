using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Sends the registration invitation email to a newly-invited
/// CorporateManager or CorporateStudent. The raw registration token is
/// included in the email — this is the single point at which the token
/// crosses an external boundary; it is never persisted.
/// Fires on <see cref="InvitationIssuedEvent"/>; uses the
/// <c>FsbsInvitationIssued</c> SES template.
/// </summary>
internal sealed class InvitationIssuedHandler(
    ISesEmailService ses,
    ILogger<InvitationIssuedHandler> logger) : INotificationHandler<InvitationIssuedEvent>
{
    private const string TemplateName = "FsbsInvitationIssued";

    public async Task HandleAsync(InvitationIssuedEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Sending InvitationIssued email for invitation {InvitationId} ({Role}) to {Email}.",
            notification.InvitationId,
            notification.InviteeRole,
            notification.InviteeEmail);

        await ses.SendTemplatedEmailAsync(notification.InviteeEmail, TemplateName, new
        {
            email            = notification.InviteeEmail,
            role             = notification.InviteeRole.ToString(),
            organisationName = notification.OrganisationName,
            token            = notification.RawToken,
            expiresAt        = notification.ExpiresAt
        }, ct);
    }
}
