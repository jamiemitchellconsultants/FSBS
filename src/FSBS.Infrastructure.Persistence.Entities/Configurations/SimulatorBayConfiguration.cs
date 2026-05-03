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

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
