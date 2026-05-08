using System.Data;
using Dapper;

namespace FSBS.Worker.Notifications;

/// <summary>
/// Dapper-based implementation of <see cref="IUserLookupService"/>.
/// Executes a single lightweight query against <c>fsbs.app_users</c> +
/// <c>fsbs.user_profiles</c> to resolve contact details for notification dispatch.
/// </summary>
internal sealed class UserLookupService(IDbConnection db) : IUserLookupService
{
    /// <summary>
    /// Executes a single Dapper query to resolve the email and display name
    /// for the given user ID. Returns <c>null</c> when the user does not exist
    /// or has been soft-deleted.
    /// </summary>
    public async Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT u.email, p.first_name, p.last_name
            FROM   fsbs.app_users    u
            JOIN   fsbs.user_profiles p ON p.user_id = u.user_id
            WHERE  u.user_id = @UserId
              AND  u.is_deleted = false
            LIMIT  1
            """;

        var row = await db.QuerySingleOrDefaultAsync<(string Email, string FirstName, string LastName)>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        if (row == default) return null;
        return new UserContact(row.Email, $"{row.FirstName} {row.LastName}");
    }
}
