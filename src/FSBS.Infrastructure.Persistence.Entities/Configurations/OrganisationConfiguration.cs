using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("org_id");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.CustomerClass).HasConversion<string>().IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Account)
            .WithOne(a => a.Organisation)
            .HasForeignKey<OrgAccount>(a => a.OrgId);
    }
}
