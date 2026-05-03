using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InstructorAvailabilityConfiguration : IEntityTypeConfiguration<InstructorAvailability>
{
    public void Configure(EntityTypeBuilder<InstructorAvailability> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("availability_id");

        builder.Property(e => e.StartAt).IsRequired();
        builder.Property(e => e.EndAt).IsRequired();
        builder.Property(e => e.AvailabilityType).HasConversion<string>().IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
