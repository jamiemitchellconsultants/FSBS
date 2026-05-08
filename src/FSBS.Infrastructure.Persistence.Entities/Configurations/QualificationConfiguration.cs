using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("qualification_id");

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IssuedDate).IsRequired();
        builder.Property(e => e.ExpiryDate);
        builder.Property(e => e.DocumentS3Key).HasMaxLength(500);
        builder.Property(e => e.VerifiedBy);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.VerifiedBy)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
