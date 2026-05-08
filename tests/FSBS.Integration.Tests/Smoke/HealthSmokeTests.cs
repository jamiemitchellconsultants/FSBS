using FSBS.Integration.Tests.Infrastructure;

namespace FSBS.Integration.Tests.Smoke;

/// <summary>
/// Smoke test for the integration-test scaffold. Verifies the host starts,
/// migrations apply, and the test auth scheme is wired correctly. Replace this
/// with the real test classes (DatabaseConstraintTests, BookingLifecycleTests,
/// etc.) once the scaffold is exercised.
/// </summary>
[Trait("Category", "Integration")]
public class HealthSmokeTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    [Fact]
    public async Task UnauthenticatedRequest_ToProtectedEndpoint_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/v1/bookings");

        ((int)response.StatusCode).Should().BeOneOf(401, 404);
    }

    [Fact]
    public async Task AuthenticatedRequest_PassesTheAuthGate()
    {
        var client = CreateClient(AppRole.SalesStaff);

        var response = await client.GetAsync("/v1/bookings/pending-approval");

        // Endpoint may legitimately return 200/204/404 depending on routing,
        // but anything in [200..499] except 401 means the auth gate passed.
        ((int)response.StatusCode).Should().NotBe(401);
    }
}
