using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InstructorWeeklyPatternSlotConfiguration : IEntityTypeConfiguration<InstructorWeeklyPatternSlot>
{
    public void Configure(EntityTypeBuilder<InstructorWeeklyPatternSlot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("slot_id");

        builder.Property(e => e.DayOfWeek)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(e => e.StartTime).HasColumnType("time").IsRequired();
        builder.Property(e => e.EndTime).HasColumnType("time").IsRequired();

        builder.HasIndex(e => e.PatternId).HasDatabaseName("ix_instructor_weekly_pattern_slots_pattern");

        builder.HasCheckConstraint(
            "ck_pattern_slot_range",
            "end_time > start_time");

        builder.HasCheckConstraint(
            "ck_pattern_slot_day",
            "day_of_week BETWEEN 0 AND 6");

        builder.HasCheckConstraint(
            "ck_pattern_slot_half_hour_aligned",
            "extract(minute from start_time) IN (0, 30) " +
            "AND extract(second from start_time) = 0 " +
            "AND extract(minute from end_time) IN (0, 30) " +
            "AND extract(second from end_time) = 0");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
