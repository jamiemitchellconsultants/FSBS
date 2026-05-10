using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class CustomerClassRefConfiguration : IEntityTypeConfiguration<CustomerClassRef>
{
    public void Configure(EntityTypeBuilder<CustomerClassRef> builder)
    {
        builder.ToTable("customer_classes");
        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasData(
            new CustomerClassRef { Code = "Standard",  Label = "Standard",          IsActive = true },
            new CustomerClassRef { Code = "Staff",      Label = "Staff",              IsActive = true },
            new CustomerClassRef { Code = "Corporate",  Label = "Corporate",          IsActive = true }
        );
    }
}
