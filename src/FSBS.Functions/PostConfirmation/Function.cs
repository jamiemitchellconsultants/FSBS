using System.Security.Cryptography;
using System.Text;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using FSBS.Functions.Common;
using Npgsql;
using NpgsqlTypes;


namespace FSBS.Functions.PostConfirmation;

/// <summary>
/// Cognito Post Confirmation Lambda trigger.
/// Provisions an <c>fsbs.users</c> row for the newly confirmed account and,
/// where applicable, marks the originating invitation <c>Claimed</c> and
/// places staff users into the matching Cognito group derived from their
/// Entra ID groups.
/// </summary>
/// <remarks>
/// Wired to <b>both</b> the staff and customer pools in
/// <c>AppStack.PostConfirmation</c>. The Lambda differentiates by inspecting
/// the user attributes:
/// <list type="bullet">
///   <item>
///     <c>custom:entra_groups</c> present → staff flow. Role is mapped from
///     the highest-priority Entra group; the user is added to the matching
///     Cognito group via <c>AdminAddUserToGroup</c>.
///   </item>
///   <item>
///     <c>custom:invitation_token</c> present → corporate invitation flow.
///     The invitation row is loaded by SHA-256 hash, the user is created with
///     the invitation's <c>invitee_role</c> and the org's <c>tenant_id</c>,
///     and the invitation row is marked <c>Claimed</c>.
///   </item>
///   <item>
///     <c>custom:registration_type == "private"</c> → private customer flow.
///     User created with role <c>PrivateCustomer</c>, no organisation.
///   </item>
/// </list>
/// </remarks>
public sealed class Function
{
    private const string EntraGroupsAttribute      = "custom:entra_groups";
    private const string InvitationTokenAttribute  = "custom:invitation_token";
    private const string RegistrationTypeAttribute = "custom:registration_type";
    private const string PrivateRegistrationType   = "private";
    private const string RootTenantIdEnvVar        = "FSBS_ROOT_TENANT_ID";

    private static readonly string[] EntraRolePriority =
    [
        "SystemAdmin", "ScheduleAdmin", "CourseDirector",
        "Management", "SalesStaff", "Instructor", "InternalStudent"
    ];

    public async Task<CognitoPostConfirmationEvent> FunctionHandler(
        CognitoPostConfirmationEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PostConfirmation trigger for user {0}, pool {1}, source {2}",
            cognitoEvent.UserName,
            cognitoEvent.UserPoolId,
            cognitoEvent.TriggerSource);

        var attributes = cognitoEvent.Request.UserAttributes;

        if (!attributes.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
        {
            context.Logger.LogWarning(
                "PostConfirmation: no email attribute on event — skipping provisioning");
            return cognitoEvent;
        }

        // Cognito sets UserName to the federated `sub` for OIDC users; for
        // native customer pool users it is the username they registered with.
        var cognitoSub       = cognitoEvent.UserName;
        var connectionString = await DbConnection.GetConnectionStringAsync();
        var rootTenantId     = Guid.Parse(RequireEnv(RootTenantIdEnvVar));

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        if (attributes.TryGetValue(EntraGroupsAttribute, out var entraGroups)
            && !string.IsNullOrWhiteSpace(entraGroups))
        {
            await ProvisionStaffAsync(
                conn, cognitoEvent, context, email, cognitoSub, entraGroups, rootTenantId);
            return cognitoEvent;
        }

        if (attributes.TryGetValue(InvitationTokenAttribute, out var rawToken)
            && !string.IsNullOrWhiteSpace(rawToken))
        {
            await ProvisionCorporateAsync(conn, context, email, cognitoSub, rawToken);
            return cognitoEvent;
        }

        if (attributes.TryGetValue(RegistrationTypeAttribute, out var regType)
            && string.Equals(regType, PrivateRegistrationType, StringComparison.Ordinal))
        {
            await ProvisionPrivateCustomerAsync(conn, context, email, cognitoSub, rootTenantId);
            return cognitoEvent;
        }

        context.Logger.LogWarning(
            "PostConfirmation: no recognisable registration flow for {0} — no user row created",
            email);
        return cognitoEvent;
    }

    private static async Task ProvisionStaffAsync(
        NpgsqlConnection conn,
        CognitoPostConfirmationEvent cognitoEvent,
        ILambdaContext context,
        string email,
        string cognitoSub,
        string entraGroups,
        Guid rootTenantId)
    {
        var role = MapEntraGroupsToRole(entraGroups);
        if (role is null)
        {
            context.Logger.LogWarning(
                "PostConfirmation: staff user {0} has no recognisable Entra group in '{1}'",
                email, entraGroups);
            return;
        }

        await UpsertUserAsync(conn, cognitoSub, email, role, rootTenantId);
        context.Logger.LogInformation(
            "PostConfirmation: staff user {0} provisioned with role {1}", email, role);

        using var idp = new AmazonCognitoIdentityProviderClient();
        await idp.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest
        {
            UserPoolId = cognitoEvent.UserPoolId,
            Username   = cognitoEvent.UserName,
            GroupName  = role
        });
        context.Logger.LogInformation(
            "PostConfirmation: added {0} to Cognito group {1}", email, role);
    }

    private static async Task ProvisionCorporateAsync(
        NpgsqlConnection conn,
        ILambdaContext context,
        string email,
        string cognitoSub,
        string rawToken)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
                               .ToLowerInvariant();

        const string invitationSql = """
            SELECT i.invitation_id, i.invitee_role::text, i.org_id, o.tenant_id
            FROM fsbs.invitations i
            JOIN fsbs.organisations o ON o.org_id = i.org_id
            WHERE i.token_hash = @token_hash AND i.status = 'Pending'
            LIMIT 1
        """;

        Guid invitationId;
        string inviteeRole;
        Guid orgId;
        Guid tenantId;

        await using (var cmd = new NpgsqlCommand(invitationSql, conn))
        {
            cmd.Parameters.AddWithValue("token_hash", tokenHash);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                context.Logger.LogWarning(
                    "PostConfirmation: invitation token not found / not pending for {0}", email);
                return;
            }

            invitationId = reader.GetGuid(0);
            inviteeRole  = reader.GetString(1);
            orgId        = reader.GetGuid(2);
            tenantId     = reader.GetGuid(3);
        }

        await using var tx = await conn.BeginTransactionAsync();
        var userId = await UpsertUserAsync(conn, cognitoSub, email, inviteeRole, tenantId, tx);

        const string membershipSql = """
            INSERT INTO fsbs.org_memberships (membership_id, user_id, org_id, role)
            VALUES (uuid_generate_v4(), @user_id, @org_id, 'Member')
            ON CONFLICT DO NOTHING
        """;
        await using (var cmd = new NpgsqlCommand(membershipSql, conn, tx))
        {
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("org_id",  orgId);
            await cmd.ExecuteNonQueryAsync();
        }

        const string claimSql = """
            UPDATE fsbs.invitations
               SET status              = 'Claimed',
                   claimed_by_user_id  = @user_id,
                   claimed_at          = now(),
                   updated_at          = now()
             WHERE invitation_id = @invitation_id AND status = 'Pending'
        """;
        await using (var cmd = new NpgsqlCommand(claimSql, conn, tx))
        {
            cmd.Parameters.AddWithValue("user_id",       userId);
            cmd.Parameters.AddWithValue("invitation_id", invitationId);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        context.Logger.LogInformation(
            "PostConfirmation: corporate user {0} provisioned (role {1}, org {2}); invitation {3} claimed",
            email, inviteeRole, orgId, invitationId);
    }

    private static async Task ProvisionPrivateCustomerAsync(
        NpgsqlConnection conn,
        ILambdaContext context,
        string email,
        string cognitoSub,
        Guid rootTenantId)
    {
        await UpsertUserAsync(conn, cognitoSub, email, "PrivateCustomer", rootTenantId);
        context.Logger.LogInformation(
            "PostConfirmation: private customer {0} provisioned", email);
    }

    private static async Task<Guid> UpsertUserAsync(
        NpgsqlConnection conn,
        string cognitoSub,
        string email,
        string role,
        Guid tenantId,
        NpgsqlTransaction? tx = null)
    {
        const string sql = """
            INSERT INTO fsbs.users (cognito_sub, email, role, tenant_id)
            VALUES (@cognito_sub, @email, @role::fsbs.app_role, @tenant_id)
            ON CONFLICT (cognito_sub) DO UPDATE
                SET email      = EXCLUDED.email,
                    role       = EXCLUDED.role,
                    tenant_id  = EXCLUDED.tenant_id,
                    updated_at = now()
            RETURNING user_id
        """;

        await using var cmd = tx is null
            ? new NpgsqlCommand(sql, conn)
            : new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("cognito_sub", cognitoSub);
        cmd.Parameters.AddWithValue("email",       email);
        cmd.Parameters.AddWithValue("role",        role);
        cmd.Parameters.Add("tenant_id", NpgsqlDbType.Uuid).Value = tenantId;

        return (Guid)(await cmd.ExecuteScalarAsync()
                      ?? throw new InvalidOperationException("Failed to insert/update user."));
    }

    private static string? MapEntraGroupsToRole(string entraGroups)
    {
        var groups = entraGroups
            .Split([',', ';', ' '],
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var role in EntraRolePriority)
        {
            if (Array.Exists(groups, g => string.Equals(g, role, StringComparison.OrdinalIgnoreCase)))
                return role;
        }

        return null;
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException(
                $"{name} environment variable is not configured.");
}
