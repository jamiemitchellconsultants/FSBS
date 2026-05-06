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
using StackExchange.Redis;

namespace FSBS.Infrastructure;

public static class InfrastructureServiceExtensions
{
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
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(region));
        services.AddScoped<IS3Service, S3Service>();

        // Redis / ElastiCache — singleton multiplexer, scoped cache wrapper.
        // Registration is skipped when Redis:ConnectionString is absent so that
        // the notification worker (which does not use the availability cache)
        // can call AddInfrastructure without a Redis connection string.
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        var redisConnStr = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnStr))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnStr));
            services.AddScoped<IAvailabilityCache, AvailabilityCache>();
        }

        return services;
    }
}
