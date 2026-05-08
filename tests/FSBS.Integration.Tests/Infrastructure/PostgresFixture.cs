using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Spins up a single PostgreSQL container for the test assembly, applies the
/// canonical <c>fsbs_schema.sql</c> DDL once, and exposes a Respawn checkpoint
/// that can truncate all fsbs-schema tables between tests in milliseconds.
/// </summary>
/// <remarks>
/// <para>
/// We apply <c>fsbs_schema.sql</c> directly rather than EF Core migrations
/// because the EF migrations were generated against a database that already
/// contained the PostgreSQL ENUM types defined in the schema file (e.g.
/// <c>training_type</c>). The schema file is the canonical source of truth as
/// documented in CLAUDE.md.
/// </para>
/// <para>
/// The container runs as the default <c>postgres</c> superuser, which bypasses
/// Row Level Security. Tests that need to assert RLS behaviour should
/// <c>SET ROLE</c> on their own connection — see <c>RowLevelSecurityTests</c>.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("fsbs_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var schemaSql = await File.ReadAllTextAsync(LocateSchemaFile());

        await using (var conn = new NpgsqlConnection(ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(schemaSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        await using var rconn = new NpgsqlConnection(ConnectionString);
        await rconn.OpenAsync();
        _respawner = await Respawner.CreateAsync(rconn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["fsbs"],
        });
    }

    /// <summary>Truncates all fsbs-schema tables in a single transaction.</summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Walks up from the test bin directory until it finds <c>fsbs_schema.sql</c>
    /// at the repo root. Avoids hardcoding the relative path so the fixture works
    /// regardless of where <c>dotnet test</c> is invoked from.
    /// </summary>
    private static string LocateSchemaFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "fsbs_schema.sql");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            "Could not find fsbs_schema.sql by walking up from the test base directory.");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
