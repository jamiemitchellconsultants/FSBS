namespace FSBS.Application.Invitations.Commands;

public record CreateCorporateManagerInvitationResult(
    Guid InvitationId,
    string InviteeEmail,
    string OrgName,
    DateTimeOffset ExpiresAt);
