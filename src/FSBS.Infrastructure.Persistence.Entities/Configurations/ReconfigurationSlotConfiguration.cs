using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class ReconfigurationSlotConfiguration : IEntityTypeConfiguration<ReconfigurationSlot>
{
    public void Configure(EntityTypeBuilder<ReconfigurationSlot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("reconfig_slot_id");

        builder.Property(e => e.StartAt).IsRequired();
        builder.Property(e => e.EndAt).IsRequired();
        builder.Property(e => e.DurationMins).IsRequired();

        builder.HasIndex(e => new { e.BayId, e.StartAt, e.EndAt })
            .IsUnique()
            .HasDatabaseName("uq_reconfig_slots_bay_time");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Bay)
            .WithMany(b => b.ReconfigurationSlots)
            .HasForeignKey(e => e.BayId);

        builder.HasOne(e => e.PrecedingBooking)
            .WithMany()
            .HasForeignKey(e => e.PrecedingBookingId)
            .IsRequired(false);

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
