using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class SimulatorBayConfiguration : IEntityTypeConfiguration<SimulatorBay>
{
    public void Configure(EntityTypeBuilder<SimulatorBay> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("bay_id");

        builder.Property(e => e.BayCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();

        builder.HasIndex(e => new { e.SimulatorUnitId, e.BayCode })
            .IsUnique()
            .HasDatabaseName("uq_simulator_bays_code");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.MaintenanceWindows)
            .WithOne(m => m.Bay)
            .HasForeignKey(m => m.BayId);
    }
}
