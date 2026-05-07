using FSBS.Domain.Enums;

namespace FSBS.Domain.Events;

/// <summary>
/// Raised when a new invitation is created by SalesStaff (CorporateManager
/// invitation) or by a CorporateManager (CorporateStudent invitation).
/// The notification worker consumes this event and sends an SES email
/// containing the raw registration token — the only point at which the raw
/// token is transmitted; it is never persisted.
/// </summary>
public sealed record InvitationIssuedEvent(
    Guid InvitationId,
    Guid OrgId,
    string OrganisationName,
    string InviteeEmail,
    InviteeRole InviteeRole,
    string RawToken,
    DateTimeOffset ExpiresAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
