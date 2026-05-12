using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

/// <summary>
/// EF Fluent API configuration for <see cref="LessonTemplate"/>. Tenant-scoped
/// (`tenant_id` column, global query filter applied in <c>FsbsDbContext</c>),
/// soft-deletable, with <c>xmin</c> concurrency token. Unique partial index on
/// <c>(tenant_id, lower(title)) WHERE is_deleted = false</c> prevents duplicate
/// active library entries per tenant.
/// </summary>
public class LessonTemplateConfiguration : IEntityTypeConfiguration<LessonTemplate>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LessonTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("lesson_template_id");

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.TrainingType).HasColumnType("fsbs.training_type").IsRequired();
        builder.Property(e => e.DefaultMinDurationMins).IsRequired();
        builder.Property(e => e.RequiresInstructor).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.IsMandatoryByDefault).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasCheckConstraint("ck_lesson_templates_duration", "default_min_duration_mins > 0");

        builder.HasIndex(nameof(LessonTemplate.TenantId), nameof(LessonTemplate.TrainingType), nameof(LessonTemplate.IsActive), nameof(LessonTemplate.IsDeleted))
            .HasDatabaseName("ix_lesson_templates_filter");

        builder.HasIndex(nameof(LessonTemplate.TenantId), nameof(LessonTemplate.Title))
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("uq_lesson_templates_tenant_title_active");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
