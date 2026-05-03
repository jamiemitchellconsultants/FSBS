using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class BookingSlotConfiguration : IEntityTypeConfiguration<BookingSlot>
{
    public void Configure(EntityTypeBuilder<BookingSlot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("slot_id");

        builder.Property(e => e.StartAt).IsRequired();
        builder.Property(e => e.EndAt).IsRequired();
        builder.Property(e => e.DurationMins).IsRequired();
        builder.Property(e => e.SlotStatus).HasConversion<string>().IsRequired();

        builder.HasCheckConstraint("ck_booking_slots_min_duration", "duration_mins >= 240");

        builder.HasIndex(e => new { e.BayId, e.StartAt, e.EndAt })
            .HasFilter("slot_status != 'Cancelled'")
            .IsUnique()
            .HasDatabaseName("uq_booking_slots_bay_time");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Bay)
            .WithMany(b => b.BookingSlots)
            .HasForeignKey(e => e.BayId);
    }
}
