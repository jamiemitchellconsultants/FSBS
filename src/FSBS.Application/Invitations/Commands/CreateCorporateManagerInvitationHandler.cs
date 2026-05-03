using System.Security.Cryptography;
using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Invitations.Commands;

public sealed class CreateCorporateManagerInvitationHandler(
    IInvitationRepository invitations,
    IOrganisationRepository organisations)
    : IRequestHandler<CreateCorporateManagerInvitationCommand, CreateCorporateManagerInvitationResult>
{
    private const int ExpiryDays = 7;

    public async Task<CreateCorporateManagerInvitationResult> Handle(
        CreateCorporateManagerInvitationCommand command,
        CancellationToken ct)
    {
        var org = await organisations.FindByIdAsync(command.OrgId, ct)
            ?? throw new OrganisationNotFoundException(command.OrgId);

        if (await invitations.HasPendingAsync(command.InviteeEmail, command.OrgId, ct))
            throw new DuplicateInvitationException(command.InviteeEmail, command.OrgId);

        // Raw token never stored — only the SHA-256 hex digest is persisted.
        var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken      = Convert.ToHexString(rawTokenBytes).ToLowerInvariant();
        var tokenHash     = Convert.ToHexString(SHA256.HashData(rawTokenBytes)).ToLowerInvariant();

        var invitation = new Invitation
        {
            Id           = Guid.NewGuid(),
            OrgId        = command.OrgId,
            InviteeEmail = command.InviteeEmail,
            InviteeRole  = InviteeRole.CorporateManager,
            TokenHash    = tokenHash,
            Status       = InvitationStatus.Pending,
            ExpiresAt    = DateTimeOffset.UtcNow.AddDays(ExpiryDays),
        };

        await invitations.CreateAsync(invitation, ct);

        // TODO: emit InvitationIssued domain event → SQS → SES email containing rawToken.
        // rawToken must NOT be logged or returned in the API response.

        return new CreateCorporateManagerInvitationResult(
            invitation.Id,
            invitation.InviteeEmail,
            org.Name,
            invitation.ExpiresAt);
    }
}
