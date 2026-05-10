using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("org_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.CustomerClass).HasMaxLength(50).IsRequired();
        builder.HasOne<CustomerClassRef>().WithMany().HasForeignKey(e => e.CustomerClass).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ContractType).HasMaxLength(50);
        builder.Property(e => e.CreditLimitGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.BillingEmail).IsRequired().HasMaxLength(255);
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.HasCheckConstraint("ck_organisations_credit_limit", "credit_limit_gbp >= 0");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Account)
            .WithOne(a => a.Organisation)
            .HasForeignKey<OrgAccount>(a => a.OrgId);
    }
}
