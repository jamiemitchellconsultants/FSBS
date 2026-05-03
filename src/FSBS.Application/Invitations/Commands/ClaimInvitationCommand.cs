using MediatR;

namespace FSBS.Application.Invitations.Commands;

/// <summary>
/// Creates an account for an invited corporate user (CorporateManager or
/// CorporateStudent) and marks the invitation as Claimed in a single atomic
/// operation. The caller's role and organisation are derived from the invitation
/// record — never trusted from the request body.
/// </summary>
/// <remarks>
/// In production this command is superseded by the Cognito Post-Confirmation
/// Lambda, which performs the same DB operations after the user confirms their
/// email via the hosted UI. In the dev environment (DevAuth:Enabled = true)
/// there is no Cognito or Lambda, so this command handles the full registration.
/// </remarks>
public record ClaimInvitationCommand(
    string Token,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IRequest<ClaimInvitationResult>;
