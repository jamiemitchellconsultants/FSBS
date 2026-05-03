using FSBS.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSBS.Infrastructure.Persistence;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<AuditInterceptor>();

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
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        return services;
    }
}
