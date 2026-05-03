using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using Npgsql;

namespace FSBS.Functions.Cognito;

/// <summary>
/// Cognito Post Confirmation Lambda trigger for the <c>fsbs-customer-pool</c>.
/// Fires after a user successfully confirms their sign-up (email code verified).
/// Creates the <c>AppUser</c> and <c>UserProfile</c> rows in the FSBS database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Trigger source filter:</b> only <c>PostConfirmation_ConfirmSignUp</c> is
/// handled. The <c>PostConfirmation_ConfirmForgotPassword</c> source is ignored
/// because no new user record is needed for a password reset.
/// </para>
/// <para>
/// <b>Tenant isolation:</b> each private customer receives their own
/// <c>tenant_id</c> (a newly generated GUID). This provides row-level isolation
/// in all tenant-scoped EF global query filters. Staff admin operations that
/// need to see all private customers must call <c>IgnoreQueryFilters()</c>.
/// </para>
/// <para>
/// <b>Database access:</b> uses raw Npgsql rather than EF Core to keep the
/// Lambda package small and avoid the full EF model build at cold-start.
/// The connection string is read from the <c>FSBS_DB_CONNECTION</c> environment
/// variable, which the CDK AppStack injects from Secrets Manager at function
/// creation time.
/// </para>
/// <para>
/// <b>Idempotency:</b> the insert uses <c>ON CONFLICT DO NOTHING</c> on the
/// <c>cognito_sub</c> unique index so that Cognito retries do not create
/// duplicate rows.
/// </para>
/// </remarks>
public sealed class PostConfirmationHandler
{
    private const string TriggerSourceConfirmSignUp = "PostConfirmation_ConfirmSignUp";

    public async Task<CognitoPostConfirmationEvent> FunctionHandler(
        CognitoPostConfirmationEvent cognitoEvent,
        ILambdaContext context)
    {
        if (cognitoEvent.TriggerSource != TriggerSourceConfirmSignUp)
        {
            context.Logger.LogInformation(
                "Skipping trigger source {TriggerSource}", cognitoEvent.TriggerSource);
            return cognitoEvent;
        }

        var attrs = cognitoEvent.Request.UserAttributes;

        attrs.TryGetValue("sub",         out var sub);
        attrs.TryGetValue("email",       out var email);
        attrs.TryGetValue("given_name",  out var firstName);
        attrs.TryGetValue("family_name", out var lastName);
        attrs.TryGetValue("phone_number", out var phoneNumber);

        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
        {
            context.Logger.LogError(
                "Missing required attributes: sub={Sub}, email={Email}", sub, email);
            throw new InvalidOperationException("Cognito event is missing required user attributes.");
        }

        var userId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid(); // Each private customer is their own isolated tenant.
        var now      = DateTimeOffset.UtcNow;

        var connectionString = Environment.GetEnvironmentVariable("FSBS_DB_CONNECTION")
            ?? throw new InvalidOperationException("FSBS_DB_CONNECTION environment variable is not set.");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await InsertAppUserAsync(conn, tx, userId, tenantId, sub, email, now, context);
            await InsertUserProfileAsync(conn, tx, userId, firstName, lastName, phoneNumber, now, context);
            await tx.CommitAsync();

            context.Logger.LogInformation(
                "Created AppUser {UserId} for Cognito sub {Sub}", userId, sub);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return cognitoEvent;
    }

    private static async Task InsertAppUserAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid userId,
        Guid tenantId,
        string cognitoSub,
        string email,
        DateTimeOffset now,
        ILambdaContext context)
    {
        const string sql = """
            INSERT INTO fsbs.app_users
                (user_id, tenant_id, cognito_sub, email, app_role, is_deleted, created_at, updated_at)
            VALUES
                (@userId, @tenantId, @cognitoSub, @email, @appRole, false, @now, @now)
            ON CONFLICT (cognito_sub) DO NOTHING
            """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("userId",     userId);
        cmd.Parameters.AddWithValue("tenantId",   tenantId);
        cmd.Parameters.AddWithValue("cognitoSub", cognitoSub);
        cmd.Parameters.AddWithValue("email",      email);
        cmd.Parameters.AddWithValue("appRole",    "PrivateCustomer");
        cmd.Parameters.AddWithValue("now",        now);

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
            context.Logger.LogWarning("AppUser row already exists for sub {Sub} — skipping insert.", cognitoSub);
    }

    private static async Task InsertUserProfileAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid userId,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        DateTimeOffset now,
        ILambdaContext context)
    {
        const string sql = """
            INSERT INTO fsbs.user_profiles
                (user_id, first_name, last_name, phone_number, created_at, updated_at)
            VALUES
                (@userId, @firstName, @lastName, @phoneNumber, @now, @now)
            ON CONFLICT (user_id) DO NOTHING
            """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("userId",      userId);
        cmd.Parameters.AddWithValue("firstName",   (object?)firstName  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lastName",    (object?)lastName   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("phoneNumber", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("now",         now);

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
            context.Logger.LogWarning("UserProfile row already exists for user {UserId} — skipping insert.", userId);
    }
}
