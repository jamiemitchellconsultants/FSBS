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
        builder.Property(e => e.Id).HasColumnName("booking_discount_id");

        builder.Property(e => e.DiscountType).HasConversion<string>().IsRequired();
        builder.Property(e => e.DiscountPct).HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.DiscountAmountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.CreatedAt)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasOne(e => e.DiscountRule)
            .WithMany()
            .HasForeignKey(e => e.DiscountRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
