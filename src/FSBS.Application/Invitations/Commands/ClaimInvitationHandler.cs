using FSBS.Application.Common.Exceptions;
using FSBS.Application.Invitations.Queries;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Invitations.Commands;

public sealed class ClaimInvitationHandler(IInvitationRepository invitations)
    : IRequestHandler<ClaimInvitationCommand, ClaimInvitationResult>
{
    public async Task<ClaimInvitationResult> Handle(
        ClaimInvitationCommand command,
        CancellationToken ct)
    {
        if (!ValidateInvitationTokenHandler.TryHashToken(command.Token, out var tokenHash))
            throw new InvitationNotFoundException();

        var invitation = await invitations.FindPendingByTokenHashAsync(tokenHash, ct)
            ?? throw new InvitationNotFoundException();

        var (appRole, orgRole) = invitation.InviteeRole switch
        {
            InviteeRole.CorporateManager => (AppRole.CorporateManager, OrgRole.Manager),
            InviteeRole.CorporateStudent => (AppRole.CorporateStudent, OrgRole.Student),
            _ => throw new InvalidOperationException(
                $"Invitation has unsupported role '{invitation.InviteeRole}'.")
        };

        var userId = Guid.NewGuid();

        var user = new AppUser
        {
            Id         = userId,
            TenantId   = invitation.Organisation.TenantId,
            CognitoSub = invitation.InviteeEmail,
            Email      = invitation.InviteeEmail,
            AppRole    = appRole,
            IsDeleted  = false,
        };

        var profile = new UserProfile
        {
            Id          = userId,
            FirstName   = command.FirstName,
            LastName    = command.LastName,
            PhoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber) ? null : command.PhoneNumber,
        };

        var membership = new OrgMembership
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            OrgId     = invitation.OrgId,
            OrgRole   = orgRole,
            IsDeleted = false,
        };

        await invitations.ClaimWithNewUserAsync(invitation, user, profile, membership, ct);

        return new ClaimInvitationResult(
            userId,
            invitation.InviteeEmail,
            invitation.OrgId,
            appRole.ToString());
    }
}
