using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("report_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.DefinitionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.OwnerId).IsRequired();
        builder.Property(e => e.IsShared).IsRequired();
        builder.Property(e => e.ScheduleCron).HasMaxLength(100);
        builder.Property(e => e.LastRunAt);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Runs)
            .WithOne(r => r.Report)
            .HasForeignKey(r => r.ReportId);
    }
}
