using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class AccountStatusRefConfiguration : IEntityTypeConfiguration<AccountStatusRef>
{
    public void Configure(EntityTypeBuilder<AccountStatusRef> builder)
    {
        builder.ToTable("account_statuses");
        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.AllowsBooking).IsRequired().HasDefaultValue(true);

        builder.HasData(
            new AccountStatusRef { Code = "Active",    Label = "Active",    IsActive = true,  AllowsBooking = true  },
            new AccountStatusRef { Code = "Suspended", Label = "Suspended", IsActive = true,  AllowsBooking = false },
            new AccountStatusRef { Code = "Closed",    Label = "Closed",    IsActive = true,  AllowsBooking = false }
        );
    }
}
