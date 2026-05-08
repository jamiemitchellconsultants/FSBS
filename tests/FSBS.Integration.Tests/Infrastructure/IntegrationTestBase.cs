using System.Net.Http.Headers;
using FSBS.Domain.Enums;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Shared base class for integration tests. Owns one
/// <see cref="FsbsWebApplicationFactory"/> per test class and resets the
/// database between tests via Respawn so state never leaks across cases.
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    protected FsbsWebApplicationFactory Factory { get; }

    protected IntegrationTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;
        Factory = new FsbsWebApplicationFactory(postgres.ConnectionString);
    }

    /// <summary>
    /// Creates a fresh <see cref="HttpClient"/> with the headers the test auth
    /// handler expects. Pass the role and identity the test should impersonate.
    /// </summary>
    protected HttpClient CreateClient(
        AppRole role,
        Guid? userId = null,
        Guid? tenantId = null,
        Guid? orgId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, (userId ?? Guid.NewGuid()).ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, (tenantId ?? Guid.NewGuid()).ToString());
        if (orgId.HasValue)
            client.DefaultRequestHeaders.Add(TestAuthHandler.OrgHeader, orgId.Value.ToString());
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public Task InitializeAsync() => _postgres.ResetAsync();

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
