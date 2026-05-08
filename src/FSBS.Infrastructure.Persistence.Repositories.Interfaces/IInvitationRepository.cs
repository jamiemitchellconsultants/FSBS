using FSBS.Domain.Entities;

namespace FSBS.Infrastructure.Persistence.Repositories.Interfaces;

public interface IInvitationRepository
{
    /// <summary>
    /// Returns <c>true</c> when a Pending invitation already exists for the
    /// given email and organisation, preventing duplicate invitations.
    /// </summary>
    Task<bool> HasPendingAsync(string inviteeEmail, Guid orgId, CancellationToken ct = default);

    /// <summary>Persists a new invitation record.</summary>
    Task CreateAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>
    /// Finds a Pending, non-expired invitation by its SHA-256 token hash.
    /// Includes the <see cref="Organisation"/> navigation property.
    /// Returns null when no matching invitation exists, or when it is expired
    /// or in any status other than Pending.
    /// </summary>
    Task<Invitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Atomically marks the invitation as Claimed and persists the new
    /// <see cref="AppUser"/>, <see cref="UserProfile"/>, and
    /// <see cref="OrgMembership"/> in a single <c>SaveChanges</c> call.
    /// </summary>
    Task ClaimWithNewUserAsync(
        Invitation invitation,
        AppUser user,
        UserProfile profile,
        OrgMembership membership,
        CancellationToken ct = default);
}
