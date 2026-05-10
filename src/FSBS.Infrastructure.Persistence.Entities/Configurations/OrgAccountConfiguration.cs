using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class OrgAccountConfiguration : IEntityTypeConfiguration<OrgAccount>
{
    public void Configure(EntityTypeBuilder<OrgAccount> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("account_id");

        builder.Property(e => e.CreditLimitGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.HasOne<AccountStatusRef>().WithMany().HasForeignKey(e => e.Status).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.PaymentTermsDays).IsRequired().HasDefaultValue(30);
        builder.HasCheckConstraint("ck_org_accounts_payment_terms", "payment_terms_days > 0");

        builder.Property(e => e.CurrentBalanceGbp)
            .HasPrecision(12, 2)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Payments)
            .WithOne(p => p.OrgAccount)
            .HasForeignKey(p => p.OrgAccountId);

        builder.HasMany(e => e.Statements)
            .WithOne()
            .HasForeignKey("OrgAccountId");
    }
}
