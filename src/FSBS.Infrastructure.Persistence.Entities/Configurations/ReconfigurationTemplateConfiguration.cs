using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ReconfigurationTemplateConfiguration : IEntityTypeConfiguration<ReconfigurationTemplate>
{
    public void Configure(EntityTypeBuilder<ReconfigurationTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("reconfig_template_id");

        builder.Property(e => e.FromConfigId).IsRequired();
        builder.Property(e => e.ToConfigId).IsRequired();
        builder.Property(e => e.DurationMins).IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes");

        builder.HasIndex(e => new { e.FromConfigId, e.ToConfigId })
            .IsUnique()
            .HasDatabaseName("uq_reconfig_templates_pair");

        builder.HasCheckConstraint("ck_reconfig_templates_different",
            "from_config_id != to_config_id");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.FromConfiguration)
            .WithMany()
            .HasForeignKey(e => e.FromConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToConfiguration)
            .WithMany()
            .HasForeignKey(e => e.ToConfigId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
