using System.Data;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Amazon.SimpleEmail;
using Amazon.SQS;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Persistence;
using Npgsql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace FSBS.Integration.Tests.Infrastructure;

/// <summary>
/// Hosts the real <c>FSBS.Api</c> against a Testcontainers PostgreSQL database
/// with all AWS SDK clients swapped for NSubstitute fakes and JWT auth replaced
/// by a header-based <see cref="TestAuthHandler"/>.
/// </summary>
public sealed class FsbsWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    public IAmazonSQS SqsMock { get; } = Substitute.For<IAmazonSQS>();
    public IAmazonSimpleEmailService SesMock { get; } = Substitute.For<IAmazonSimpleEmailService>();
    public IAmazonS3 S3Mock { get; } = Substitute.For<IAmazonS3>();
    public IAmazonCognitoIdentityProvider CognitoMock { get; } = Substitute.For<IAmazonCognitoIdentityProvider>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Development" so the API registers its `Dev` JWT scheme path (the
        // production branch demands real Cognito config we don't have in CI).
        // We then strip Dev auth and replace it with `TestAuthHandler` below.
        builder.UseEnvironment("Development");

        // Disable startup-time DI graph validation. The production wiring has
        // unresolved registrations (e.g. Domain.Interfaces.IBookingRepository
        // has no implementation registered in AddRepositories) that would
        // otherwise abort host construction. Tests that depend on those
        // services will still surface the missing registration at request
        // time with a clear error.
        builder.UseDefaultServiceProvider(opts =>
        {
            opts.ValidateOnBuild = false;
            opts.ValidateScopes = false;
        });

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Fsbs"] = connectionString,
                // Required by Program.cs Development branch even though the Dev
                // scheme will be overridden by the test scheme.
                ["DevAuth:Secret"] = "test-secret-do-not-use-in-prod-test-secret",
                ["DevAuth:Issuer"] = "fsbs-tests",
                ["DevAuth:Enabled"] = "true",
                // Empty Redis connection — Program.cs and AddInfrastructure
                // both skip backplane/cache registration when this is missing.
                ["Redis:ConnectionString"] = "",
                ["Aws:Region"] = "eu-west-1",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace AWS SDK singletons with mocks. Use Replace() so the prior
            // registrations from AddInfrastructure are evicted, not stacked.
            services.Replace(ServiceDescriptor.Singleton(SqsMock));
            services.Replace(ServiceDescriptor.Singleton(SesMock));
            services.Replace(ServiceDescriptor.Singleton(S3Mock));
            services.Replace(ServiceDescriptor.Singleton(CognitoMock));

            // Register fakes for services the production wiring leaves
            // unregistered when Redis is absent. Without these, route-argument
            // inference fails at host startup because IAvailabilityCache and
            // IAvailabilityReadService appear as endpoint parameters.
            services.AddSingleton(Substitute.For<IAvailabilityCache>());
            services.AddSingleton(Substitute.For<IAvailabilityReadService>());

            // Dapper read service in production is constructed in Worker only;
            // provide a real Postgres connection per scope so the API host can
            // resolve any IDbConnection dependency.
            services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

            // Swap the default authentication scheme to the test header handler.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultScheme = TestAuthHandler.SchemeName;
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            // Override the default authorization policy so it accepts the test
            // scheme. Named role policies (RequireSalesStaff, etc.) keep working
            // because they only check claim values, not the source scheme.
            services.PostConfigure<AuthorizationOptions>(opts =>
            {
                opts.DefaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
            });
        });
    }

    /// <summary>
    /// Resolves a scoped service from the host's DI container. Useful for
    /// seeding the database directly via <c>FsbsDbContext</c> in arrange steps.
    /// </summary>
    public async Task<T> GetScopedAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = Services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public async Task SeedAsync(Func<FsbsDbContext, Task> seed)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FsbsDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }
}
