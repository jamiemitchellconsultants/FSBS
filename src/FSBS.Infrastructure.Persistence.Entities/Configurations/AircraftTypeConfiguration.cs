using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class AircraftTypeConfiguration : IEntityTypeConfiguration<AircraftType>
{
    public void Configure(EntityTypeBuilder<AircraftType> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("aircraft_type_id");

        builder.Property(e => e.IcaoCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(e => e.IcaoCode)
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("uq_aircraft_types_icao_code");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Configurations)
            .WithOne(c => c.AircraftType)
            .HasForeignKey(c => c.AircraftTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
