using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Events;

namespace FSBS.Worker.Notifications.Handlers;

/// <summary>
/// Sends a welcome / registration-complete email to the invitee after they
/// successfully claim their invitation and complete Cognito sign-up.
/// Fires on <see cref="InvitationClaimedEvent"/>; uses the
/// <c>FsbsInvitationClaimed</c> SES template.
/// </summary>
internal sealed class InvitationIssuedHandler(
    ISesEmailService ses,
    IUserLookupService users,
    ILogger<InvitationIssuedHandler> logger) : INotificationHandler<InvitationClaimedEvent>
{
    private const string TemplateName = "FsbsInvitationClaimed";

    public async Task HandleAsync(InvitationClaimedEvent notification, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Sending InvitationClaimed welcome email for user {UserId} (org {OrgId}).",
            notification.ClaimedBy, notification.OrgId);

        var user = await users.GetContactAsync(notification.ClaimedBy, ct);
        if (user is null)
        {
            logger.LogWarning(
                "InvitationClaimed: user {UserId} not found — skipping welcome email.",
                notification.ClaimedBy);
            return;
        }

        await ses.SendTemplatedEmailAsync(user.Email, TemplateName, new
        {
            name        = user.DisplayName,
            role        = notification.InviteeRole.ToString(),
            orgId       = notification.OrgId,
            claimedAt   = notification.OccurredAt
        }, ct);
    }
}
