namespace FSBS.Application.Invitations.Commands;

/// <summary>
/// Returned after a CorporateManager or CorporateStudent invitation has been
/// successfully created. The raw token is delivered solely via SES email and
/// is never included in this result.
/// </summary>
public record CreateCorporateManagerInvitationResult(
    Guid InvitationId,
    string InviteeEmail,
    string OrgName,
    DateTimeOffset ExpiresAt);
