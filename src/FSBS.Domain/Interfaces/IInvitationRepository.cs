using FSBS.Domain.Entities;

namespace FSBS.Domain.Interfaces;

/// <summary>
/// Write-side repository for the Invitation aggregate. Used by command handlers.
/// </summary>
public interface IInvitationRepository
{
    Task<Invitation?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Finds a Pending, non-expired invitation by its SHA-256 token hash.
    /// Returns null if no matching invitation exists or if it has expired/been claimed.
    /// </summary>
    Task<Invitation?> FindPendingByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<bool> HasPendingAsync(string inviteeEmail, Guid orgId, CancellationToken ct = default);

    Task AddAsync(Invitation invitation, CancellationToken ct = default);
}
