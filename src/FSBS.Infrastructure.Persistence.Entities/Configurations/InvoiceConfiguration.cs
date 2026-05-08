using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("invoice_id");

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.GrossGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.DiscountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.NetGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.IssuedDate).IsRequired();
        builder.Property(e => e.DueDate);
        builder.Property(e => e.PaidAt);

        builder.HasCheckConstraint("ck_invoices_net", "net_gbp = gross_gbp - discount_gbp");
        builder.HasCheckConstraint("ck_invoices_amounts", "gross_gbp >= 0 AND discount_gbp >= 0 AND net_gbp >= 0");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Booking)
            .WithMany()
            .HasForeignKey(e => e.BookingId);

        builder.HasOne(e => e.Organisation)
            .WithMany()
            .HasForeignKey(e => e.OrgId);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Allocations)
            .WithOne(a => a.Invoice)
            .HasForeignKey(a => a.InvoiceId);
    }
}
