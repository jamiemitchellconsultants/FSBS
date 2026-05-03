using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using FSBS.Infrastructure.Persistence.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence.Repositories;

internal sealed class InvitationRepository(FsbsDbContext db) : IInvitationRepository
{
    public Task<bool> HasPendingAsync(string inviteeEmail, Guid orgId, CancellationToken ct = default) =>
        db.Invitations.AnyAsync(
            i => i.InviteeEmail == inviteeEmail
              && i.OrgId        == orgId
              && i.Status       == InvitationStatus.Pending,
            ct);

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
