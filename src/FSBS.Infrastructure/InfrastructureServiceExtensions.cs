using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Amazon.SimpleEmail;
using Amazon.SQS;
using FSBS.Application.Common.Interfaces;
using FSBS.Infrastructure.Availability;
using FSBS.Infrastructure.Cognito;
using FSBS.Infrastructure.Email;
using FSBS.Infrastructure.Messaging;
using FSBS.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FSBS.Infrastructure;

/// <summary>
/// DI registration extension for all infrastructure services: Cognito, SQS, SES, S3,
/// and the Redis availability cache. Registers stubs when <c>DevAuth:Enabled</c> is
/// true so local development requires no AWS credentials.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds Cognito, SQS, SES, S3, and Redis services to the service collection.
    /// Binds settings from the <c>Cognito</c>, <c>Sqs</c>, <c>Ses</c>, <c>S3</c>,
    /// and <c>Redis</c> configuration sections.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var region = RegionEndpoint.GetBySystemName(
            configuration["Aws:Region"] ?? "eu-west-1");

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
                new AmazonCognitoIdentityProviderClient(region));
            services.AddScoped<ICognitoService, CognitoService>();
        }
        services.AddScoped<ICognitoHostedUiService, CognitoHostedUiService>();

        // SQS
        services.Configure<SqsSettings>(configuration.GetSection("Sqs"));
        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(region));
        services.AddScoped<ISqsPublisher, SqsPublisher>();

        // SES
        services.Configure<SesSettings>(configuration.GetSection("Ses"));
        services.AddSingleton<IAmazonSimpleEmailService>(_ =>
            new AmazonSimpleEmailServiceClient(region));
        services.AddScoped<ISesEmailService, SesEmailService>();

        // S3
        services.Configure<S3Settings>(configuration.GetSection("S3"));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var s3Settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;
            if (!string.IsNullOrWhiteSpace(s3Settings.ServiceUrl))
            {
                return new AmazonS3Client(
                    "test", "test",
                    new AmazonS3Config
                    {
                        ServiceURL           = s3Settings.ServiceUrl,
                        ForcePathStyle       = s3Settings.ForcePathStyle,
                        AuthenticationRegion = configuration["Aws:Region"] ?? "eu-west-1"
                    });
            }
            return new AmazonS3Client(region);
        });
        services.AddScoped<IS3Service, S3Service>();

        // Redis / ElastiCache — singleton multiplexer when configured, scoped
        // cache wrapper. When Redis:ConnectionString is absent we register a
        // no-op IAvailabilityCache so consumers (e.g. SimulatorEndpoints) can
        // still resolve their dependencies in dev/test without Redis. The
        // no-op behaves like a permanent cache miss, so reads fall through to
        // the underlying read service unchanged.
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        var redisConnStr = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnStr))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnStr));
            services.AddScoped<IAvailabilityCache, AvailabilityCache>();
        }
        else
        {
            services.AddScoped<IAvailabilityCache, NoOpAvailabilityCache>();
        }

        return services;
    }
}
