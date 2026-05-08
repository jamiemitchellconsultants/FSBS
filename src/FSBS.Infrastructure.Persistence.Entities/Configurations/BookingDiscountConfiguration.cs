using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class BookingDiscountConfiguration : IEntityTypeConfiguration<BookingDiscount>
{
    public void Configure(EntityTypeBuilder<BookingDiscount> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("discount_id");

        builder.Property(e => e.DiscountType).HasConversion<string>().IsRequired();
        builder.Property(e => e.DiscountPct).HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.AmountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasCheckConstraint("ck_booking_discounts_pct", "discount_pct >= 0 AND discount_pct <= 100");
        builder.HasCheckConstraint("ck_booking_discounts_amount", "amount_gbp >= 0");

        // booking_discounts is an immutable audit snapshot written once at
        // confirmation time. All properties are locked after the initial insert.
        builder.Property(e => e.DiscountType)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(e => e.DiscountPct)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(e => e.AmountGbp)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(e => e.CreatedAt)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.HasOne(e => e.DiscountRule)
            .WithMany()
            .HasForeignKey(e => e.DiscountRuleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
