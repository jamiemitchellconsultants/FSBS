namespace FSBS.Worker.Notifications;

/// <summary>
/// Lightweight read-only service used by notification handlers to resolve
/// user contact details (email, display name) from a user ID.
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Returns the email address and display name for the given user, or
    /// <c>null</c> if the user does not exist.
    /// </summary>
    Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Email address and display name for a user.</summary>
public sealed record UserContact(string Email, string DisplayName);
