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

        builder.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.MaxHoursPerWeek).IsRequired();
        builder.Property(e => e.HireDate).IsRequired();
        builder.Property(e => e.TrainingTypeRatings).HasColumnType("fsbs.training_type[]").IsRequired();
        builder.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("uq_instructors_user");
        builder.HasIndex(e => e.EmployeeNumber).IsUnique().HasDatabaseName("uq_instructors_employee_number");
        builder.HasCheckConstraint("ck_instructors_hours", "max_hours_per_week > 0");

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
