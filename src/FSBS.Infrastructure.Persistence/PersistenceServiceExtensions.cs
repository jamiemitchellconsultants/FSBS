using System.Data;
using FSBS.Domain.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FSBS.Infrastructure.Persistence;

/// <summary>
/// DI registration extension for the EF Core persistence layer.
/// Registers <see cref="FsbsDbContext"/>, interceptors, <see cref="IUnitOfWork"/>,
/// and a scoped <see cref="System.Data.IDbConnection"/> for Dapper read services.
/// </summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    /// Adds the EF Core <see cref="FsbsDbContext"/>, audit and tenant interceptors,
    /// <see cref="IUnitOfWork"/>, and a Dapper <see cref="System.Data.IDbConnection"/>
    /// to the service collection.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = ResolveConnectionString(config);

        // Scoped (not singleton) so ICurrentUser is resolved from the correct
        // request scope and the correct authenticated user is stamped on every write.
        services.AddScoped<AuditInterceptor>();
        // Scoped so the correct tenant_id claim is read per-request for RLS enforcement.
        services.AddScoped<TenantCommandInterceptor>();

        services.AddDbContext<FsbsDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__ef_migrations_history", "fsbs");
                        npgsql.MigrationsAssembly("FSBS.Infrastructure.Persistence.Migrations");
                        npgsql.MapEnum<TrainingType>("training_type", "fsbs");
                    })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantCommandInterceptor>()));

        // Scoped so the unit of work shares the request-scoped FsbsDbContext.
        services.AddScoped<IUnitOfWork, FsbsUnitOfWork>();

        // Dapper read services (e.g. AvailabilityReadService) take an IDbConnection
        // directly. Scoped lifetime gives one connection per request, opened lazily
        // by Dapper and disposed when the request scope ends.
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

        return services;
    }

    /// <summary>
    /// Returns the Npgsql connection string. In local development this is taken
    /// directly from <c>ConnectionStrings:Fsbs</c> in appsettings. In ECS the
    /// individual <c>FSBS_DB_*</c> env vars injected by the task definition are
    /// assembled into a connection string instead.
    /// </summary>
    private static string ResolveConnectionString(IConfiguration config)
    {
        var direct = config.GetConnectionString("Fsbs");
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        var host     = config["FSBS_DB_HOST"]     ?? throw new InvalidOperationException("FSBS_DB_HOST is not configured.");
        var port     = config["FSBS_DB_PORT"]     ?? "5432";
        var database = config["FSBS_DB_NAME"]     ?? "fsbs";
        var username = config["FSBS_DB_USERNAME"] ?? throw new InvalidOperationException("FSBS_DB_USERNAME is not configured.");
        var password = config["FSBS_DB_PASSWORD"] ?? throw new InvalidOperationException("FSBS_DB_PASSWORD is not configured.");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
