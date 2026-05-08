using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("booking_id");

        builder.Property(e => e.BookerRole).HasConversion<string>().IsRequired();
        builder.Property(e => e.TrainingType).HasColumnType("fsbs.training_type").IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.StudentCount).IsRequired();
        builder.Property(e => e.GrossPriceGbp).HasPrecision(12, 2);
        builder.Property(e => e.DiscountGbp).HasPrecision(12, 2);
        builder.Property(e => e.NetPriceGbp).HasPrecision(12, 2);
        builder.Property(e => e.DepartmentName).HasMaxLength(100);
        builder.Property(e => e.BudgetCode).HasMaxLength(50);
        builder.Property(e => e.IdempotencyKey).IsRequired();

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uq_bookings_idempotency_key");

        builder.HasCheckConstraint("ck_bookings_fd_capacity",
            "training_type != 'flight_deck' OR student_count <= 4");

        builder.HasCheckConstraint("ck_bookings_cc_capacity",
            "training_type != 'cabin_crew' OR student_count <= 10");

        builder.HasCheckConstraint("ck_bookings_student_count",
            "student_count >= 1");

        builder.HasCheckConstraint("ck_bookings_discount_pct",
            "discount_gbp IS NULL OR (discount_gbp >= 0 AND discount_gbp <= gross_price_gbp)");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Configuration)
            .WithMany()
            .HasForeignKey(e => e.ConfigurationId);

        builder.HasMany(e => e.Slots)
            .WithOne(s => s.Booking)
            .HasForeignKey(s => s.BookingId);

        builder.HasMany(e => e.Notes)
            .WithOne(n => n.Booking)
            .HasForeignKey(n => n.BookingId);

        builder.HasMany(e => e.Discounts)
            .WithOne(d => d.Booking)
            .HasForeignKey(d => d.BookingId);

        builder.HasOne(e => e.Approval)
            .WithOne(a => a.Booking)
            .HasForeignKey<BookingApproval>(a => a.BookingId);
    }
}
