using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class SimulatorUnitConfiguration : IEntityTypeConfiguration<SimulatorUnit>
{
    public void Configure(EntityTypeBuilder<SimulatorUnit> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("simulator_unit_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.DefaultReconfigMins).IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.ActiveConfiguration)
            .WithMany()
            .HasForeignKey(e => e.ActiveConfigurationId)
            .IsRequired(false);

        builder.HasMany(e => e.Bays)
            .WithOne(b => b.SimulatorUnit)
            .HasForeignKey(b => b.SimulatorUnitId);

        builder.HasMany(e => e.MaintenanceWindows)
            .WithOne(m => m.SimulatorUnit)
            .HasForeignKey(m => m.SimulatorUnitId);
    }
}
