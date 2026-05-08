using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class AccountStatementConfiguration : IEntityTypeConfiguration<AccountStatement>
{
    public void Configure(EntityTypeBuilder<AccountStatement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("statement_id");

        builder.Property(e => e.OrgId).IsRequired();
        builder.Property(e => e.GeneratedAt).IsRequired();
        builder.Property(e => e.GeneratedBy).IsRequired();
        builder.Property(e => e.PeriodStart).IsRequired();
        builder.Property(e => e.PeriodEnd).IsRequired();
        builder.Property(e => e.OpeningBalanceGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.ClosingBalanceGbp).HasPrecision(12, 2).IsRequired();
        builder.Property(e => e.StatementS3Key).IsRequired().HasMaxLength(500);

        builder.HasOne(e => e.Organisation)
            .WithMany()
            .HasForeignKey(e => e.OrgId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
