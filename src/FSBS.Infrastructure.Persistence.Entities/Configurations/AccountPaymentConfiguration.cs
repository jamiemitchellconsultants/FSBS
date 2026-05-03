using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class AccountPaymentConfiguration : IEntityTypeConfiguration<AccountPayment>
{
    public void Configure(EntityTypeBuilder<AccountPayment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("payment_id");

        builder.Property(e => e.AmountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.PaymentMethod).HasConversion<string>().IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.VoidReason).HasMaxLength(500);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Allocations)
            .WithOne(a => a.Payment)
            .HasForeignKey(a => a.PaymentId);
    }
}
