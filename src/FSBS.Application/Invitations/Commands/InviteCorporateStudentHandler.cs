using System.Security.Cryptography;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Invitations.Commands;

public sealed class InviteCorporateStudentHandler(
    ICurrentUser currentUser,
    IInvitationRepository invitations,
    IOrganisationRepository organisations,
    ISqsPublisher sqs)
    : IRequestHandler<InviteCorporateStudentCommand, CreateCorporateManagerInvitationResult>
{
    private const int ExpiryDays = 7;

    /// <inheritdoc/>
    public async Task<CreateCorporateManagerInvitationResult> Handle(
        InviteCorporateStudentCommand command,
        CancellationToken ct)
    {
        // OrgId must come from the caller's JWT — never from the request body.
        var orgId = currentUser.OrgId
            ?? throw new InvalidOperationException(
                "CorporateManager token is missing the org_id claim.");

        var org = await organisations.FindByIdAsync(orgId, ct)
            ?? throw new OrganisationNotFoundException(orgId);

        if (await invitations.HasPendingAsync(command.InviteeEmail, orgId, ct))
            throw new DuplicateInvitationException(command.InviteeEmail, orgId);

        var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken      = Convert.ToHexString(rawTokenBytes).ToLowerInvariant();
        var tokenHash     = Convert.ToHexString(SHA256.HashData(rawTokenBytes)).ToLowerInvariant();

        var invitation = new Invitation
        {
            Id           = Guid.NewGuid(),
            OrgId        = orgId,
            InviteeEmail = command.InviteeEmail,
            InviteeRole  = InviteeRole.CorporateStudent,
            TokenHash    = tokenHash,
            Status       = InvitationStatus.Pending,
            ExpiresAt    = DateTimeOffset.UtcNow.AddDays(ExpiryDays),
        };

        await invitations.CreateAsync(invitation, ct);

        await sqs.PublishAsync(new InvitationIssuedEvent(
            InvitationId:     invitation.Id,
            OrgId:             invitation.OrgId,
            OrganisationName:  org.Name,
            InviteeEmail:      invitation.InviteeEmail,
            InviteeRole:       invitation.InviteeRole,
            RawToken:          rawToken,
            ExpiresAt:         invitation.ExpiresAt), ct);

        return new CreateCorporateManagerInvitationResult(
            invitation.Id,
            invitation.InviteeEmail,
            org.Name,
            invitation.ExpiresAt);
    }
}
