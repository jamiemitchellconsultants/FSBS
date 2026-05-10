using MediatR;

namespace FSBS.Application.Invitations.Commands;

/// <summary>
/// Issued by a CorporateManager to invite a new CorporateStudent into their
/// own organisation. The handler derives the OrgId from the caller's JWT rather
/// than accepting it from the client, preventing cross-org invitation forgery.
/// </summary>
public record InviteCorporateStudentCommand(string InviteeEmail, string? PersonalNote = null)
    : IRequest<CreateCorporateManagerInvitationResult>;
