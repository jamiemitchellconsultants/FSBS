using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the Invitation aggregate. Used by command handlers.
/// </summary>
public interface IInvitationRepository
{
    /// <summary>Returns the invitation by its primary key, or null if not found.</summary>
    Task<Invitation?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Finds a Pending, non-expired invitation by its SHA-256 token hash.
    /// Returns null if no matching invitation exists or if it has expired/been claimed.
    /// </summary>
    Task<Invitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Returns true if a Pending, non-expired invitation already exists for the
    /// given email/org pair. Used to enforce the unique-active-invitation constraint
    /// before creating a new one.
    /// </summary>
    Task<bool> HasPendingAsync(string inviteeEmail, Guid orgId, CancellationToken ct = default);

    /// <summary>Adds the invitation to the change tracker for insertion on next SaveChanges.</summary>
    Task AddAsync(Invitation invitation, CancellationToken ct = default);
}
