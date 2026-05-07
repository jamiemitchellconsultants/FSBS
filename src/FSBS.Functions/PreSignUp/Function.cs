using System.Security.Cryptography;
using System.Text;
using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using FSBS.Functions.Common;
using Npgsql;


namespace FSBS.Functions.PreSignUp;

/// <summary>
/// Cognito Pre Sign-up Lambda trigger for the <c>fsbs-customer-pool</c>.
/// Decides whether to admit a new sign-up attempt by inspecting the user
/// attributes supplied by the hosted UI / SDK.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <c>custom:registration_type == "private"</c> — private customer
///     self-registration. Allowed unconditionally; no invitation token required.
///   </item>
///   <item>
///     Otherwise the sign-up is treated as a corporate invitation flow. A
///     <c>custom:invitation_token</c> attribute must be supplied. The Lambda
///     SHA-256-hashes the presented token and looks for a matching row in
///     <c>fsbs.invitations</c> that is <c>Pending</c>, not yet expired, and
///     whose <c>invitee_email</c> matches the address being registered. Any
///     mismatch causes the sign-up to be rejected.
///   </item>
/// </list>
/// Wired via <c>AppStack.PreSignUp</c> on the customer pool only — never the
/// staff pool.
/// </remarks>
public sealed class Function
{
    private const string RegistrationTypeAttribute = "custom:registration_type";
    private const string InvitationTokenAttribute  = "custom:invitation_token";
    private const string PrivateRegistrationType   = "private";

    public async Task<CognitoPreSignupEvent> FunctionHandler(
        CognitoPreSignupEvent cognitoEvent, ILambdaContext context)
    {
        context.Logger.LogInformation(
            "PreSignUp trigger for user {0}, source {1}",
            cognitoEvent.UserName,
            cognitoEvent.TriggerSource);

        cognitoEvent.Request.UserAttributes.TryGetValue(
            RegistrationTypeAttribute, out var registrationType);

        if (registrationType == PrivateRegistrationType)
        {
            context.Logger.LogInformation(
                "Allowing private customer registration for {0}",
                cognitoEvent.UserName);
            cognitoEvent.Response.AutoVerifyEmail = true;
            return cognitoEvent;
        }

        if (!cognitoEvent.Request.UserAttributes.TryGetValue(
                InvitationTokenAttribute, out var rawToken)
            || string.IsNullOrWhiteSpace(rawToken))
        {
            context.Logger.LogWarning(
                "Blocking sign-up for {0}: no invitation token supplied",
                cognitoEvent.UserName);
            throw new Exception(
                "Registration requires a valid invitation token. " +
                "Please use the link sent to your email address.");
        }

        cognitoEvent.Request.UserAttributes.TryGetValue("email", out var email);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception("Registration requires an email address.");
        }

        var tokenHash = HashToken(rawToken);

        await ValidateInvitationAsync(tokenHash, email, context);

        cognitoEvent.Response.AutoVerifyEmail = true;
        return cognitoEvent;
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
               .ToLowerInvariant();

    private static async Task ValidateInvitationAsync(
        string tokenHash, string email, ILambdaContext context)
    {
        var connectionString = await DbConnection.GetConnectionStringAsync();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = """
            SELECT invitee_email, expires_at, status::text
            FROM fsbs.invitations
            WHERE token_hash = @token_hash
            LIMIT 1
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token_hash", tokenHash);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            context.Logger.LogWarning(
                "PreSignUp: no invitation found for the supplied token hash");
            throw new Exception("Invitation not found or has been revoked.");
        }

        var inviteeEmail = reader.GetString(0);
        var expiresAt    = reader.GetFieldValue<DateTimeOffset>(1);
        var status       = reader.GetString(2);

        if (!string.Equals(status, "Pending", StringComparison.Ordinal))
        {
            context.Logger.LogWarning(
                "PreSignUp: invitation status is {0}, expected Pending", status);
            throw new Exception("Invitation has already been used, expired, or revoked.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            context.Logger.LogWarning(
                "PreSignUp: invitation expired at {0}", expiresAt);
            throw new Exception("Invitation has expired. Please request a new invitation.");
        }

        if (!string.Equals(inviteeEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.LogWarning(
                "PreSignUp: email mismatch — invitation issued to a different address");
            throw new Exception("Invitation email does not match the registration email.");
        }
    }
}
