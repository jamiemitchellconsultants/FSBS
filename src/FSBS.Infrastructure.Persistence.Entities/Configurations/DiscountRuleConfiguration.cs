using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("discount_rule_id");

        builder.Property(e => e.DiscountType).HasMaxLength(50).IsRequired();
        builder.HasOne<DiscountTypeRef>().WithMany().HasForeignKey(e => e.DiscountType).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.Priority).IsRequired();
        builder.Property(e => e.DiscountPct).HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.IsCombinable).IsRequired();
        builder.Property(e => e.ThresholdJson).HasColumnType("jsonb");
        builder.HasCheckConstraint("ck_discount_rules_pct", "discount_pct >= 0 AND discount_pct <= 100");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
