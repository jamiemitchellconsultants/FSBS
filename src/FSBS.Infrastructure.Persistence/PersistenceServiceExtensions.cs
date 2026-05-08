using System.Data;
using FSBS.Domain.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FSBS.Infrastructure.Persistence;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        // Scoped (not singleton) so ICurrentUser is resolved from the correct
        // request scope and the correct authenticated user is stamped on every write.
        services.AddScoped<AuditInterceptor>();
        // Scoped so the correct tenant_id claim is read per-request for RLS enforcement.
        services.AddScoped<TenantCommandInterceptor>();

        services.AddDbContext<FsbsDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    config.GetConnectionString("Fsbs")
                        ?? throw new InvalidOperationException("Connection string 'Fsbs' is not configured."),
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
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(
            config.GetConnectionString("Fsbs")
                ?? throw new InvalidOperationException("Connection string 'Fsbs' is not configured.")));

        return services;
    }
}
