using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class BookingApprovalConfiguration : IEntityTypeConfiguration<BookingApproval>
{
    public void Configure(EntityTypeBuilder<BookingApproval> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("approval_id");

        builder.Property(e => e.Decision).HasConversion<string>().IsRequired();
        builder.Property(e => e.RejectionReason).HasMaxLength(2000);

        builder.HasCheckConstraint("ck_booking_approvals_no_self_approval",
            "requested_by != reviewed_by");

        builder.HasCheckConstraint("ck_booking_approvals_rejection",
            "decision != 'Rejected' OR (rejection_reason IS NOT NULL AND char_length(rejection_reason) >= 10)");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
