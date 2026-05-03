using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("invitation_id");

        builder.Property(e => e.InviteeEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.InviteeRole).HasConversion<string>().IsRequired();
        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();

        builder.HasIndex(e => e.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_invitations_token_hash");

        builder.HasIndex(e => new { e.InviteeEmail, e.OrgId })
            .HasFilter("status = 'Pending'")
            .IsUnique()
            .HasDatabaseName("uq_invitations_pending_email_org");

        builder.HasCheckConstraint("ck_invitations_claimed",
            "(status != 'Claimed' OR (claimed_by IS NOT NULL AND claimed_at IS NOT NULL))");

        builder.HasCheckConstraint("ck_invitations_revoked",
            "(status != 'Revoked' OR (revoked_by IS NOT NULL AND revoked_at IS NOT NULL))");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Organisation)
            .WithMany(o => o.Invitations)
            .HasForeignKey(e => e.OrgId);
    }
}
