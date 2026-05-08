using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ReportRunConfiguration : IEntityTypeConfiguration<ReportRun>
{
    public void Configure(EntityTypeBuilder<ReportRun> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("run_id");

        builder.Property(e => e.TriggeredBy).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.StartedAt);
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.ResultS3Key).HasMaxLength(500);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.TriggeredBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
