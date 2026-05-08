using Npgsql;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for tests that hit PostgreSQL directly (constraint and trigger
/// tests) without needing the API host. Skips <see cref="FsbsWebApplicationFactory"/>
/// to keep these tests fast.
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    protected DatabaseTestBase(PostgresFixture postgres) => _postgres = postgres;

    protected string ConnectionString => _postgres.ConnectionString;

    protected Task<NpgsqlConnection> OpenConnectionAsync() =>
        DbSeeder.OpenAsync(ConnectionString);

    public Task InitializeAsync() => _postgres.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Helpers for asserting <see cref="PostgresException"/>s in tests.
/// </summary>
internal static class PostgresExceptionAssertions
{
    public const string CheckViolation = "23514";
    public const string UniqueViolation = "23505";
    public const string NotNullViolation = "23502";
}
