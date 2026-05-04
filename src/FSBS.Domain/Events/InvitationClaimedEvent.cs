using FSBS.Domain.Enums;

namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a corporate invitation is claimed during registration.
/// Triggers the Post Confirmation Lambda flow to assign the user's Cognito group.
/// </summary>
public record InvitationClaimedEvent(
    Guid InvitationId,
    Guid OrgId,
    Guid ClaimedBy,
    InviteeRole InviteeRole) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
