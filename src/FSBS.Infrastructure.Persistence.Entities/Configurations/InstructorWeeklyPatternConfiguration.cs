using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InstructorWeeklyPatternConfiguration : IEntityTypeConfiguration<InstructorWeeklyPattern>
{
    public void Configure(EntityTypeBuilder<InstructorWeeklyPattern> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("pattern_id");

        builder.Property(e => e.EffectiveFrom).IsRequired();
        builder.Property(e => e.EffectiveTo);

        builder.HasIndex(e => e.InstructorId).HasDatabaseName("ix_instructor_weekly_patterns_instructor");

        // At most one currently-open pattern per instructor.
        builder.HasIndex(e => e.InstructorId)
            .IsUnique()
            .HasDatabaseName("uq_instructor_open_pattern")
            .HasFilter("effective_to IS NULL AND is_deleted = false");

        builder.HasOne(e => e.Instructor)
            .WithMany()
            .HasForeignKey(e => e.InstructorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Slots)
            .WithOne(s => s.Pattern)
            .HasForeignKey(s => s.PatternId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
