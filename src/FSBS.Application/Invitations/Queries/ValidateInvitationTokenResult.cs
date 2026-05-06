namespace FSBS.Application.Invitations.Queries;

/// <summary>
/// The outcome of a token-validation check. When <see cref="IsValid"/> is true
/// the remaining properties are populated and safe to display on the registration
/// form. When false all string properties are null.
/// </summary>
public record ValidateInvitationTokenResult(
    bool IsValid,
    string? InviteeEmail,
    string? OrgName,
    string? Role);
