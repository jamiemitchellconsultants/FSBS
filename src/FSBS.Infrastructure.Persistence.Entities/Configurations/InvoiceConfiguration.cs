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

        builder.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.GrossGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.DiscountGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.NetGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.IssuedOn).IsRequired();
        builder.Property(e => e.DueOn).IsRequired();

        builder.HasCheckConstraint("ck_invoices_net", "net_gbp = gross_gbp - discount_gbp");

        builder.HasIndex(e => e.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("uq_invoices_number");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Booking)
            .WithMany()
            .HasForeignKey(e => e.BookingId);

        builder.HasOne(e => e.Organisation)
            .WithMany()
            .HasForeignKey(e => e.OrgId);

        builder.HasMany(e => e.Allocations)
            .WithOne(a => a.Invoice)
            .HasForeignKey(a => a.InvoiceId);
    }
}
