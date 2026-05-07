using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using FSBS.Functions.Common;
using Npgsql;


namespace FSBS.Functions.TokenRefresh;

/// <summary>
/// Cognito Pre Token Generation Lambda trigger for the <c>fsbs-staff-pool</c>.
/// Re-syncs Cognito group membership and the <c>app_role</c> / <c>tenant_id</c>
/// JWT claims from the source of truth on every token issuance.
/// </summary>
/// <remarks>
/// <para>
/// On each call this Lambda:
/// <list type="number">
///   <item>
///     Reads <c>custom:entra_groups</c> from the event and maps it to the
///     highest-priority FSBS role.
///   </item>
///   <item>
///     Synchronises Cognito group membership so the user is in exactly the
///     group matching that role (and removed from any stale FSBS groups).
///   </item>
///   <item>
///     Looks up the user's <c>tenant_id</c> from <c>fsbs.users</c> and
///     overrides the outgoing token claims with the canonical
///     <c>app_role</c> + <c>tenant_id</c> values.
///   </item>
/// </list>
/// </para>
/// <para>
/// The Microsoft Graph integration that triggers <c>AdminUserGlobalSignOut</c>
/// when an Entra account is disabled is not implemented here — federated
/// sign-in itself fails for disabled accounts, so the residual risk is limited
/// to the lifetime of an outstanding refresh token.
/// </para>
/// <para>
/// Wired via <c>AppStack.PreTokenGeneration</c> on the staff pool only — never
/// the customer pool.
/// </para>
/// </remarks>
public sealed class Function
{
    private const string EntraGroupsAttribute = "custom:entra_groups";

    private static readonly string[] FsbsStaffRoles =
    [
        "SystemAdmin", "ScheduleAdmin", "CourseDirector",
        "Management", "SalesStaff", "Instructor", "InternalStudent"
    ];

    public async Task<CognitoPreTokenGenerationEvent> FunctionHandler(
        CognitoPreTokenGenerationEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PreTokenGeneration trigger for user {0}, pool {1}, source {2}",
            cognitoEvent.UserName,
            cognitoEvent.UserPoolId,
            cognitoEvent.TriggerSource);

        var attributes = cognitoEvent.Request.UserAttributes;

        if (!attributes.TryGetValue(EntraGroupsAttribute, out var entraGroups)
            || string.IsNullOrWhiteSpace(entraGroups))
        {
            context.Logger.LogWarning(
                "PreTokenGeneration: no custom:entra_groups attribute — skipping sync");
            return cognitoEvent;
        }

        var role = MapEntraGroupsToRole(entraGroups);
        if (role is null)
        {
            context.Logger.LogWarning(
                "PreTokenGeneration: no FSBS role mapped from entra_groups '{0}'", entraGroups);
            return cognitoEvent;
        }

        await SyncCognitoGroupAsync(cognitoEvent, role, context);

        var tenantId = await LookupTenantIdAsync(cognitoEvent.UserName, context);

        cognitoEvent.Response = new CognitoPreTokenGenerationResponse
        {
            ClaimsOverrideDetails = new ClaimOverrideDetails
            {
                ClaimsToAddOrOverride = new Dictionary<string, string>
                {
                    ["app_role"]  = role,
                    ["tenant_id"] = tenantId?.ToString() ?? string.Empty
                },
                GroupOverrideDetails = new GroupConfiguration
                {
                    GroupsToOverride = new List<string> { role }
                }
            }
        };

        return cognitoEvent;
    }

    private static async Task SyncCognitoGroupAsync(
        CognitoPreTokenGenerationEvent cognitoEvent, string targetRole, ILambdaContext context)
    {
        using var idp = new AmazonCognitoIdentityProviderClient();

        var existing = await idp.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest
        {
            UserPoolId = cognitoEvent.UserPoolId,
            Username   = cognitoEvent.UserName
        });

        var currentRoleGroups = existing.Groups
            .Where(g => Array.Exists(FsbsStaffRoles,
                r => string.Equals(r, g.GroupName, StringComparison.Ordinal)))
            .Select(g => g.GroupName)
            .ToList();

        var alreadyCorrect = currentRoleGroups.Count == 1 && currentRoleGroups[0] == targetRole;
        if (alreadyCorrect)
            return;

        // Remove stale FSBS group memberships first.
        foreach (var group in currentRoleGroups.Where(g => g != targetRole))
        {
            await idp.AdminRemoveUserFromGroupAsync(new AdminRemoveUserFromGroupRequest
            {
                UserPoolId = cognitoEvent.UserPoolId,
                Username   = cognitoEvent.UserName,
                GroupName  = group
            });
            context.Logger.LogInformation(
                "PreTokenGeneration: removed {0} from stale Cognito group {1}",
                cognitoEvent.UserName, group);
        }

        if (!currentRoleGroups.Contains(targetRole))
        {
            await idp.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest
            {
                UserPoolId = cognitoEvent.UserPoolId,
                Username   = cognitoEvent.UserName,
                GroupName  = targetRole
            });
            context.Logger.LogInformation(
                "PreTokenGeneration: added {0} to Cognito group {1}",
                cognitoEvent.UserName, targetRole);
        }
    }

    private static async Task<Guid?> LookupTenantIdAsync(string cognitoSub, ILambdaContext context)
    {
        string connectionString;
        try
        {
            connectionString = await DbConnection.GetConnectionStringAsync();
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogWarning(
                "PreTokenGeneration: cannot resolve tenant_id — {0}", ex.Message);
            return null;
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT tenant_id FROM fsbs.users WHERE cognito_sub = @sub LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("sub", cognitoSub);

        var result = await cmd.ExecuteScalarAsync();
        return result is Guid guid ? guid : null;
    }

    private static string? MapEntraGroupsToRole(string entraGroups)
    {
        var groups = entraGroups
            .Split([',', ';', ' '],
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var role in FsbsStaffRoles)
        {
            if (Array.Exists(groups, g => string.Equals(g, role, StringComparison.OrdinalIgnoreCase)))
                return role;
        }

        return null;
    }
}
