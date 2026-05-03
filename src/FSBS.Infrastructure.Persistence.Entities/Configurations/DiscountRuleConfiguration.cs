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

        builder.Property(e => e.DiscountType).HasConversion<string>().IsRequired();
        builder.Property(e => e.Priority).IsRequired();
        builder.Property(e => e.DiscountPct).HasPrecision(5, 2).IsRequired();
        builder.Property(e => e.ThresholdJson).HasColumnType("jsonb");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
