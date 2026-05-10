using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class OrgMembershipConfiguration : IEntityTypeConfiguration<OrgMembership>
{
    public void Configure(EntityTypeBuilder<OrgMembership> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("membership_id");

        builder.Property(e => e.OrgRole).HasColumnType("fsbs.org_role").IsRequired();

        builder.HasIndex(e => new { e.UserId, e.OrgId })
            .IsUnique()
            .HasDatabaseName("uq_org_memberships_user_org");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.User)
            .WithMany(u => u.OrgMemberships)
            .HasForeignKey(e => e.UserId);

        builder.HasOne(e => e.Organisation)
            .WithMany(o => o.Memberships)
            .HasForeignKey(e => e.OrgId);
    }
}
