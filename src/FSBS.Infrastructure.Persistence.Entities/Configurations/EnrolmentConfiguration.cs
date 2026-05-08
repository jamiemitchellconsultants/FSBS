using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("enrolment_id");

        builder.Property(e => e.OrgId);
        builder.Property(e => e.EnrolledAt).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().IsRequired();
        builder.Property(e => e.CompletedAt);

        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("uq_enrolments_user_course");

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(e => e.OrgId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ProgressRecords)
            .WithOne(p => p.Enrolment)
            .HasForeignKey(p => p.EnrolmentId);
    }
}
