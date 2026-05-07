using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;


namespace FSBS.Functions.DbGrants;

/// <summary>
/// CloudFormation Custom Resource handler that provisions the
/// <c>fsbs_app</c> (DML) and <c>fsbs_readonly</c> (SELECT) roles on the RDS
/// PostgreSQL instance after the database is created and the EF Core
/// migrations have run.
/// </summary>
/// <remarks>
/// <para>
/// Invoked by the CDK <c>Provider</c> framework. The Lambda runs inside the
/// VPC isolated subnet so it can reach RDS, and reads the master credentials
/// from Secrets Manager — no long-lived credentials are baked into the
/// CloudFormation template.
/// </para>
/// <para>
/// Inputs (via <c>ResourceProperties</c>):
/// <list type="bullet">
///   <item><c>DbHost</c>, <c>DbPort</c>, <c>DbName</c> — RDS endpoint details.</item>
///   <item><c>MasterSecretArn</c> — Secrets Manager ARN for the RDS admin user.</item>
///   <item><c>AppSecretArn</c> — Secrets Manager ARN for the runtime <c>fsbs_app</c> user.</item>
///   <item><c>ReadonlySecretArn</c> — optional ARN for the <c>fsbs_readonly</c> user.</item>
/// </list>
/// </para>
/// <para>
/// The grant statements are idempotent — re-running on stack update is safe.
/// Delete events are no-ops (roles are retained intentionally; dropping them
/// would orphan ECS tasks still using the secret).
/// </para>
/// </remarks>
public sealed class Function
{
    public async Task<Dictionary<string, object>> FunctionHandler(
        JsonElement evt, ILambdaContext context)
    {
        var requestType = evt.GetProperty("RequestType").GetString() ?? "Create";
        context.Logger.LogInformation("DbGrants custom resource event: {0}", requestType);

        if (string.Equals(requestType, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, object>
            {
                ["PhysicalResourceId"] = "fsbs-db-grants",
                ["Data"] = new Dictionary<string, string> { ["Status"] = "Skipped" }
            };
        }

        var props        = evt.GetProperty("ResourceProperties");
        var dbHost       = props.GetProperty("DbHost").GetString()!;
        var dbPort       = int.Parse(props.GetProperty("DbPort").GetString()!);
        var dbName       = props.GetProperty("DbName").GetString()!;
        var masterArn    = props.GetProperty("MasterSecretArn").GetString()!;
        var appArn       = props.GetProperty("AppSecretArn").GetString()!;
        var readonlyArn  = props.TryGetProperty("ReadonlySecretArn", out var ro) ? ro.GetString() : null;

        using var sm = new AmazonSecretsManagerClient();
        var master   = await ReadCredentialsAsync(sm, masterArn);
        var app      = await ReadCredentialsAsync(sm, appArn);
        var ronly    = string.IsNullOrEmpty(readonlyArn) ? null : await ReadCredentialsAsync(sm, readonlyArn);

        var masterConn = new NpgsqlConnectionStringBuilder
        {
            Host     = dbHost,
            Port     = dbPort,
            Database = dbName,
            Username = master.Username,
            Password = master.Password,
            SslMode  = SslMode.Require
        }.ConnectionString;

        await using var conn = new NpgsqlConnection(masterConn);
        await conn.OpenAsync();
        await ApplyGrantsAsync(conn, app, ronly, context);

        return new Dictionary<string, object>
        {
            ["PhysicalResourceId"] = "fsbs-db-grants",
            ["Data"] = new Dictionary<string, string> { ["Status"] = "Applied" }
        };
    }

    private static async Task ApplyGrantsAsync(
        NpgsqlConnection conn,
        DbCreds app,
        DbCreds? readonlyCreds,
        ILambdaContext context)
    {
        // Create or update the application role with the rotated password.
        await ExecAsync(conn, $"""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{app.Username}') THEN
                    CREATE ROLE {app.Username} LOGIN PASSWORD '{Escape(app.Password)}';
                ELSE
                    ALTER ROLE {app.Username} WITH LOGIN PASSWORD '{Escape(app.Password)}';
                END IF;
            END
            $$;
        """);

        await ExecAsync(conn, $"GRANT USAGE ON SCHEMA fsbs TO {app.Username};");
        await ExecAsync(conn, $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA fsbs TO {app.Username};");
        await ExecAsync(conn, $"GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA fsbs TO {app.Username};");
        await ExecAsync(conn, $"""
            ALTER DEFAULT PRIVILEGES IN SCHEMA fsbs
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {app.Username};
        """);
        await ExecAsync(conn, $"""
            ALTER DEFAULT PRIVILEGES IN SCHEMA fsbs
            GRANT USAGE, SELECT ON SEQUENCES TO {app.Username};
        """);

        context.Logger.LogInformation("DbGrants: provisioned role {0}", app.Username);

        if (readonlyCreds is not null)
        {
            await ExecAsync(conn, $"""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{readonlyCreds.Username}') THEN
                        CREATE ROLE {readonlyCreds.Username} LOGIN PASSWORD '{Escape(readonlyCreds.Password)}';
                    ELSE
                        ALTER ROLE {readonlyCreds.Username} WITH LOGIN PASSWORD '{Escape(readonlyCreds.Password)}';
                    END IF;
                END
                $$;
            """);

            await ExecAsync(conn, $"GRANT USAGE ON SCHEMA fsbs TO {readonlyCreds.Username};");
            await ExecAsync(conn, $"GRANT SELECT ON ALL TABLES IN SCHEMA fsbs TO {readonlyCreds.Username};");
            await ExecAsync(conn, $"""
                ALTER DEFAULT PRIVILEGES IN SCHEMA fsbs
                GRANT SELECT ON TABLES TO {readonlyCreds.Username};
            """);

            context.Logger.LogInformation("DbGrants: provisioned role {0}", readonlyCreds.Username);
        }
    }

    private static async Task ExecAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<DbCreds> ReadCredentialsAsync(IAmazonSecretsManager sm, string arn)
    {
        var response = await sm.GetSecretValueAsync(new GetSecretValueRequest { SecretId = arn });
        var doc      = JsonDocument.Parse(response.SecretString);
        var root     = doc.RootElement;
        return new DbCreds(
            root.GetProperty("username").GetString()!,
            root.GetProperty("password").GetString()!);
    }

    // Postgres password literals are single-quote delimited; double any embedded quotes.
    private static string Escape(string value) => value.Replace("'", "''");

    private sealed record DbCreds(string Username, string Password);
}
