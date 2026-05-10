using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("user_id");

        builder.Property(e => e.CognitoSub).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.AppRole).HasConversion<string>().IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(e => e.CognitoSub).IsUnique().HasDatabaseName("uq_app_users_cognito_sub");
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_app_users_email");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.Id);
    }
}
