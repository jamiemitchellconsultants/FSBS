namespace FSBS.Application.Invitations.Commands;

public record ClaimInvitationResult(
    Guid UserId,
    string Email,
    Guid OrgId,
    string Role);
