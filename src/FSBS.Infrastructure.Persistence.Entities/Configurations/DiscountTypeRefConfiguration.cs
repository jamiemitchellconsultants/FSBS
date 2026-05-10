using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class DiscountTypeRefConfiguration : IEntityTypeConfiguration<DiscountTypeRef>
{
    public void Configure(EntityTypeBuilder<DiscountTypeRef> builder)
    {
        builder.ToTable("discount_types");
        builder.HasKey(e => e.Code);
        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasData(
            new DiscountTypeRef { Code = "VolumeAdvanceBlock",   Label = "Volume Advance Block",    IsActive = true },
            new DiscountTypeRef { Code = "VolumeOrgSession",     Label = "Volume Org Session",       IsActive = true },
            new DiscountTypeRef { Code = "AdvanceBooking",       Label = "Advance Booking",          IsActive = true },
            new DiscountTypeRef { Code = "CorporateNegotiated",  Label = "Corporate Negotiated",     IsActive = true },
            new DiscountTypeRef { Code = "StaffRate",            Label = "Staff Rate",               IsActive = true },
            new DiscountTypeRef { Code = "Promotional",          Label = "Promotional",              IsActive = true }
        );
    }
}
