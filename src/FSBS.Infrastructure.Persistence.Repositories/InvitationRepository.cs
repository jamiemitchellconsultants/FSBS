using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IInvitationRepository"/>.
/// Uses <c>IgnoreQueryFilters</c> on unauthenticated paths to bypass the
/// tenant query filter that would otherwise match nothing for anonymous callers.
/// </summary>
internal sealed class InvitationRepository(FsbsDbContext db) : IInvitationRepository
{
    /// <summary>
    /// Returns <c>true</c> when a Pending invitation already exists for the
    /// given email and organisation, preventing duplicate invitations.
    /// </summary>
    public Task<bool> HasPendingAsync(string inviteeEmail, Guid orgId, CancellationToken ct = default) =>
        db.Invitations.AnyAsync(
            i => i.InviteeEmail == inviteeEmail
              && i.OrgId        == orgId
              && i.Status       == InvitationStatus.Pending,
            ct);

    /// <summary>Persists a new invitation record.</summary>
    public async Task CreateAsync(Invitation invitation, CancellationToken ct = default)
    {
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(ct);
    }

    public Task<Invitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        // IgnoreQueryFilters: this is a public, unauthenticated path — the tenant
        // filter on Organisation would match nothing with an empty TenantId claim.
        db.Invitations
            .IgnoreQueryFilters()
            .Include(i => i.Organisation)
            .FirstOrDefaultAsync(
                i => i.TokenHash == tokenHash
                  && i.Status    == InvitationStatus.Pending
                  && i.ExpiresAt >  DateTimeOffset.UtcNow,
                ct);

    public async Task ClaimWithNewUserAsync(
        Invitation invitation,
        AppUser user,
        UserProfile profile,
        OrgMembership membership,
        CancellationToken ct = default)
    {
        invitation.Status    = InvitationStatus.Claimed;
        invitation.ClaimedBy = user.Id;
        invitation.ClaimedAt = DateTimeOffset.UtcNow;

        db.AppUsers.Add(user);
        db.UserProfiles.Add(profile);
        db.OrgMemberships.Add(membership);

        await db.SaveChangesAsync(ct);
    }
}
