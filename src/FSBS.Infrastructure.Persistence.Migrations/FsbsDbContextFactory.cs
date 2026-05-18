using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace FSBS.Infrastructure.Persistence.Migrations;

public class FsbsDbContextFactory : IDesignTimeDbContextFactory<FsbsDbContext>
{
    public FsbsDbContext CreateDbContext(string[] args)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host     = Environment.GetEnvironmentVariable("FSBS_DB_HOST")     ?? "localhost",
            Port     = int.Parse(Environment.GetEnvironmentVariable("FSBS_DB_PORT") ?? "5432"),
            Database = Environment.GetEnvironmentVariable("FSBS_DB_NAME")     ?? "fsbs",
            Username = Environment.GetEnvironmentVariable("FSBS_DB_USERNAME") ?? "postgres",
            Password = Environment.GetEnvironmentVariable("FSBS_DB_PASSWORD") ?? "localdev"
        };

        var connectionString = csb.ConnectionString;

        var options = new DbContextOptionsBuilder<FsbsDbContext>()
            .UseNpgsql(
                connectionString,
                o =>
                {
                    o.MigrationsHistoryTable("__ef_migrations_history", "fsbs");
                    o.MigrationsAssembly(typeof(FsbsDbContextFactory).Assembly.GetName().Name);
                    o.MapEnum<TrainingType>("training_type", "fsbs");
                    o.MapEnum<InvitationStatus>("invitation_status", "fsbs");
                    o.MapEnum<InviteeRole>("invitee_role", "fsbs");
                    o.MapEnum<AvailabilityType>("availability_type", "fsbs");
                    o.MapEnum<BayStatus>("bay_status", "fsbs");
                    o.MapEnum<OrgRole>("org_role", "fsbs");
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
