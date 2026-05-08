using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ScheduleTemplateConfiguration : IEntityTypeConfiguration<ScheduleTemplate>
{
    public void Configure(EntityTypeBuilder<ScheduleTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("schedule_template_id");

        builder.Property(e => e.BayId).IsRequired();
        builder.Property(e => e.ConfigId).IsRequired();
        builder.Property(e => e.DayOfWeek).IsRequired();
        builder.Property(e => e.OpenTime).IsRequired();
        builder.Property(e => e.CloseTime).IsRequired();
        builder.Property(e => e.ValidFrom).IsRequired();
        builder.Property(e => e.ValidTo);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Bay)
            .WithMany()
            .HasForeignKey(e => e.BayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Configuration)
            .WithMany(c => c.ScheduleTemplates)
            .HasForeignKey(e => e.ConfigId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
