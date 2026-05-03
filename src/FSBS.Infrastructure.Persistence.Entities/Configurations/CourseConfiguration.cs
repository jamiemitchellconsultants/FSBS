using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("course_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.TrainingType).HasConversion<string>().IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Modules)
            .WithOne(m => m.Course)
            .HasForeignKey(m => m.CourseId);

        builder.HasMany(e => e.Enrolments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId);
    }
}
