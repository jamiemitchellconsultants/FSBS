using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FSBS.Infrastructure.Persistence.Migrations;

public class FsbsDbContextFactory : IDesignTimeDbContextFactory<FsbsDbContext>
{
    public FsbsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FsbsDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=fsbs;Username=postgres;Password=localdev",
                o =>
                {
                    o.MigrationsHistoryTable("__ef_migrations_history", "fsbs");
                    o.MigrationsAssembly(typeof(FsbsDbContextFactory).Assembly.GetName().Name);
                })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new FsbsDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public Guid TenantId => Guid.Empty;
        public Guid? OrgId => null;
        public AppRole Role => AppRole.SystemAdmin;
        public bool IsAuthenticated => false;
    }
}
