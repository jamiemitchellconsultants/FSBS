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

        builder.Property(e => e.OrgId).IsRequired();
        builder.Property(e => e.AmountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.PaymentDate).IsRequired();
        builder.Property(e => e.RecordedBy).IsRequired();
        builder.Property(e => e.PaymentMethod).HasConversion<string>().IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.Reference).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.VoidReason).HasMaxLength(500);
        builder.HasCheckConstraint("ck_account_payments_amount", "amount_gbp > 0");
        builder.HasOne<Organisation>().WithMany().HasForeignKey(e => e.OrgId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.RecordedBy).OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Allocations)
            .WithOne(a => a.Payment)
            .HasForeignKey(a => a.PaymentId);
    }
}
