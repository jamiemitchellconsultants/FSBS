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

        builder.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.RegulatoryFramework).HasMaxLength(100);
        builder.Property(e => e.TotalHours).HasPrecision(6, 1).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.TrainingType).HasColumnType("fsbs.training_type").IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasCheckConstraint("ck_courses_total_hours", "total_hours > 0");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Modules)
            .WithOne(m => m.Course)
            .HasForeignKey(m => m.CourseId);

        builder.HasMany(e => e.Enrolments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId);
    }
}
