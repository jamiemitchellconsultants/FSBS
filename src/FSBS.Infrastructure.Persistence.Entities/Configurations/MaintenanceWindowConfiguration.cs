using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class MaintenanceWindowConfiguration : IEntityTypeConfiguration<MaintenanceWindow>
{
    public void Configure(EntityTypeBuilder<MaintenanceWindow> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("maintenance_window_id");

        builder.Property(e => e.BayId).IsRequired();
        builder.Property(e => e.StartAt).IsRequired();
        builder.Property(e => e.EndAt).IsRequired();
        builder.Property(e => e.Reason).IsRequired().HasColumnType("text");

        builder.HasCheckConstraint("ck_maintenance_windows_range", "end_at > start_at");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Bay)
            .WithMany(b => b.MaintenanceWindows)
            .HasForeignKey(e => e.BayId);
    }
}
