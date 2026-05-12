using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("lesson_id");

        builder.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(e => e.SequenceOrder).IsRequired();
        builder.Property(e => e.MinDurationMins).IsRequired();
        builder.Property(e => e.RequiresInstructor).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.IsMandatory).IsRequired().HasDefaultValue(true);
        builder.HasIndex(e => new { e.ModuleId, e.SequenceOrder }).IsUnique().HasDatabaseName("uq_lessons_module_sequence");
        builder.HasCheckConstraint("ck_lessons_sequence", "sequence_order >= 1");

        builder.Property(e => e.SourceTemplateId).HasColumnName("source_template_id");
        builder.HasOne(e => e.SourceTemplate)
            .WithMany()
            .HasForeignKey(e => e.SourceTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
