using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;

namespace FSBS.Functions.Common;

/// <summary>
/// Builds a Postgres connection string for Lambda invocations. Username and
/// password are pulled fresh from Secrets Manager on every cold start so the
/// 30-day rotation of <c>fsbs/rds/app</c> is picked up automatically.
/// </summary>
internal static class DbConnection
{
    private const string DbSecretArnEnvVar = "FSBS_DB_SECRET_ARN";
    private const string DbHostEnvVar      = "FSBS_DB_HOST";
    private const string DbPortEnvVar      = "FSBS_DB_PORT";
    private const string DbNameEnvVar      = "FSBS_DB_NAME";

    private static string? _cachedConnectionString;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public static async Task<string> GetConnectionStringAsync()
    {
        if (_cachedConnectionString is not null)
            return _cachedConnectionString;

        await Lock.WaitAsync();
        try
        {
            if (_cachedConnectionString is not null)
                return _cachedConnectionString;

            var secretArn = RequireEnv(DbSecretArnEnvVar);
            var host      = RequireEnv(DbHostEnvVar);
            var port      = int.Parse(RequireEnv(DbPortEnvVar));
            var dbName    = RequireEnv(DbNameEnvVar);

            using var sm = new AmazonSecretsManagerClient();
            var response = await sm.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretArn
            });

            var doc      = JsonDocument.Parse(response.SecretString);
            var username = doc.RootElement.GetProperty("username").GetString()!;
            var password = doc.RootElement.GetProperty("password").GetString()!;

            _cachedConnectionString = new NpgsqlConnectionStringBuilder
            {
                Host     = host,
                Port     = port,
                Database = dbName,
                Username = username,
                Password = password,
                SslMode  = SslMode.Require
            }.ConnectionString;

            return _cachedConnectionString;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException(
                $"{name} environment variable is not configured.");
}
