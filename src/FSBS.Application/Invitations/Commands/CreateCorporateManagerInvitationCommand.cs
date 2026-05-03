using MediatR;

namespace FSBS.Application.Invitations.Commands;

/// <summary>
/// Issued by SalesStaff to invite a new CorporateManager to the platform.
/// The handler generates a cryptographically random token, stores only its
/// SHA-256 hash, and creates a Pending invitation record.
/// </summary>
public record CreateCorporateManagerInvitationCommand(
    string InviteeEmail,
    Guid OrgId) : IRequest<CreateCorporateManagerInvitationResult>;
