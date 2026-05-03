using MediatR;

namespace FSBS.Application.Invitations.Queries;

/// <summary>
/// Validates a raw invitation token before showing the registration form.
/// Returns a safe result (IsValid = false) for any invalid, expired, or
/// already-claimed token — never reveals which specific condition applies.
/// </summary>
public record ValidateInvitationTokenQuery(string Token)
    : IRequest<ValidateInvitationTokenResult>;
