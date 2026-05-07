using FSBS.Domain.Interfaces;
using FSBS.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                    })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantCommandInterceptor>()));

        // Scoped so the unit of work shares the request-scoped FsbsDbContext.
        services.AddScoped<IUnitOfWork, FsbsUnitOfWork>();

        return services;
    }
}
