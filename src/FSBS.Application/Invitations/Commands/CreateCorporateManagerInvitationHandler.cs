using System.Security.Cryptography;
using FSBS.Application.Common.Exceptions;
using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Domain.Events;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using MediatR;

namespace FSBS.Application.Invitations.Commands;

public sealed class CreateCorporateManagerInvitationHandler(
    ICurrentUser currentUser,
    IInvitationRepository invitations,
    IOrganisationRepository organisations,
    ISqsPublisher sqs)
    : IRequestHandler<CreateCorporateManagerInvitationCommand, CreateCorporateManagerInvitationResult>
{
    private const int ExpiryDays = 7;

    /// <inheritdoc/>
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
            IssuedBy     = currentUser.UserId,
            IssuedAt     = DateTimeOffset.UtcNow,
            PersonalNote = command.PersonalNote,
        };

        await invitations.CreateAsync(invitation, ct);

        // Publish to SQS so the worker can email the rawToken to the invitee.
        // rawToken is transmitted exactly once, here, and never persisted or
        // returned in the API response.
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
