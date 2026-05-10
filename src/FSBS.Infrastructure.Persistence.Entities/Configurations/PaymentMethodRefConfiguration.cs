using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class PaymentMethodRefConfiguration : IEntityTypeConfiguration<PaymentMethodRef>
{
    public void Configure(EntityTypeBuilder<PaymentMethodRef> builder)
    {
        builder.ToTable("payment_methods");
        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasData(
            new PaymentMethodRef { Code = "BankTransfer", Label = "Bank Transfer", IsActive = true },
            new PaymentMethodRef { Code = "Cheque",       Label = "Cheque",        IsActive = true },
            new PaymentMethodRef { Code = "Cash",         Label = "Cash",          IsActive = true },
            new PaymentMethodRef { Code = "CreditNote",   Label = "Credit Note",   IsActive = true },
            new PaymentMethodRef { Code = "Adjustment",   Label = "Adjustment",    IsActive = true }
        );
    }
}
