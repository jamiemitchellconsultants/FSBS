using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("module_id");

        builder.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(300);
        builder.Property(e => e.SequenceOrder).IsRequired();
        builder.HasIndex(e => new { e.CourseId, e.SequenceOrder }).IsUnique().HasDatabaseName("uq_modules_course_sequence");
        builder.HasCheckConstraint("ck_modules_sequence", "sequence_order >= 1");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Lessons)
            .WithOne(l => l.Module)
            .HasForeignKey(l => l.ModuleId);
    }
}
