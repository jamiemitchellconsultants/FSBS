using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class PricingPolicyConfiguration : IEntityTypeConfiguration<PricingPolicy>
{
    public void Configure(EntityTypeBuilder<PricingPolicy> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("policy_id");

        builder.Property(e => e.TrainingType).HasColumnType("fsbs.training_type").IsRequired();
        builder.Property(e => e.CustomerClass).HasMaxLength(50).IsRequired();
        builder.HasOne<CustomerClassRef>().WithMany().HasForeignKey(e => e.CustomerClass).OnDelete(DeleteBehavior.Restrict);
        builder.Property(e => e.HourlyRateGbp).HasPrecision(10, 2).IsRequired();
        builder.Property(e => e.EffectiveFrom).IsRequired();
        builder.Property(e => e.EffectiveTo);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasMany(e => e.DiscountRules)
            .WithOne(r => r.PricingPolicy)
            .HasForeignKey(r => r.PricingPolicyId);
    }
}
