using Amazon;
using Amazon.CognitoIdentityProvider;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Cognito;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Cognito settings bound from "Cognito" config section.
        services.Configure<CognitoSettings>(configuration.GetSection("Cognito"));

        var devMode = configuration.GetValue<bool>("DevAuth:Enabled");

        if (devMode)
        {
            // Stub replaces real Cognito so local dev needs no AWS credentials.
            services.AddScoped<ICognitoService, StubCognitoService>();
        }
        else
        {
            // Cognito SDK client — singleton because the underlying HTTP client is
            // thread-safe and expensive to create per-request.
            services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
            {
                var region = configuration["Cognito:Region"] ?? "eu-west-1";
                return new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(region));
            });

            services.AddScoped<ICognitoService, CognitoService>();
        }

        return services;
    }
}
