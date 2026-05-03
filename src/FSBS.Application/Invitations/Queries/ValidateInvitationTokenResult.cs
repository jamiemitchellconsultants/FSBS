namespace FSBS.Application.Invitations.Queries;

public record ValidateInvitationTokenResult(
    bool IsValid,
    string? InviteeEmail,
    string? OrgName,
    string? Role);
