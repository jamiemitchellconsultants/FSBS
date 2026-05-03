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
        builder.Property(e => e.Id).HasColumnName("org_account_id");

        builder.Property(e => e.CreditLimitGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();

        builder.Property(e => e.CurrentBalanceGbp)
            .HasPrecision(12, 2)
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.Payments)
            .WithOne(p => p.OrgAccount)
            .HasForeignKey(p => p.OrgAccountId);

        builder.HasMany(e => e.Statements)
            .WithOne(s => s.OrgAccount)
            .HasForeignKey(s => s.OrgAccountId);
    }
}
