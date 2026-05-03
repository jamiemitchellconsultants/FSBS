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

        builder.Property(e => e.GeneratedAt).IsRequired();
        builder.Property(e => e.GeneratedBy).IsRequired();
        builder.Property(e => e.StatementJson).HasColumnType("jsonb").IsRequired();
    }
}
