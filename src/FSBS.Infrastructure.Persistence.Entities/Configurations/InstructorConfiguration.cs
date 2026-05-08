using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("instructor_id");

        builder.Property(e => e.TrainingTypeRatings).HasColumnType("fsbs.training_type[]").IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);

        builder.HasMany(e => e.Availabilities)
            .WithOne(a => a.Instructor)
            .HasForeignKey(a => a.InstructorId);

        builder.HasMany(e => e.BookingSlots)
            .WithOne(s => s.Instructor)
            .HasForeignKey(s => s.InstructorId);
    }
}
