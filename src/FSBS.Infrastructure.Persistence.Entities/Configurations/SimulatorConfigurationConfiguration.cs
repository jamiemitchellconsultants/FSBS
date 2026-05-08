using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class SimulatorConfigurationConfiguration : IEntityTypeConfiguration<SimulatorConfiguration>
{
    public void Configure(EntityTypeBuilder<SimulatorConfiguration> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("config_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.AircraftType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ConfigMode).HasConversion<string>().IsRequired();
        builder.Property(e => e.SupportedTrainingTypes).HasColumnType("fsbs.training_type[]").IsRequired();
        builder.Property(e => e.MaxCapacityFlightDeck).IsRequired();
        builder.Property(e => e.MaxCapacityCabinCrew).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasCheckConstraint("ck_simulator_config_fd_capacity", "max_capacity_flight_deck > 0 AND max_capacity_flight_deck <= 4");
        builder.HasCheckConstraint("ck_simulator_config_cc_capacity", "max_capacity_cabin_crew > 0 AND max_capacity_cabin_crew <= 10");
        builder.HasCheckConstraint("ck_simulator_config_training_types", "array_length(supported_training_types, 1) >= 1");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.SimulatorUnit)
            .WithMany(u => u.Configurations)
            .HasForeignKey(e => e.SimulatorUnitId);

        builder.HasMany(e => e.ScheduleTemplates)
            .WithOne(t => t.Configuration)
            .HasForeignKey(t => t.ConfigId);

        builder.HasMany(e => e.PricingPolicies)
            .WithOne(p => p.Configuration)
            .HasForeignKey(p => p.ConfigurationId);
    }
}
