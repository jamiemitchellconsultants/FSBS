namespace FSBS.Application.Invitations.Commands;

/// <summary>
/// The result returned after a corporate user successfully claims an invitation
/// and creates their account. Returned to the API caller so it can issue a
/// session token or redirect to the sign-in page.
/// </summary>
public record ClaimInvitationResult(
    Guid UserId,
    string Email,
    Guid OrgId,
    string Role);
