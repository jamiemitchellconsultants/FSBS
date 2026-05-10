using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("user_id");

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PhoneNumber).HasMaxLength(30);
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(e => e.LicenceNumber).HasColumnName("licence_number").HasMaxLength(50);
        builder.Property(e => e.LicenceExpiry).HasColumnName("licence_expiry");
        builder.Property(e => e.PhotoS3Key).HasColumnName("photo_s3_key").HasMaxLength(500);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
