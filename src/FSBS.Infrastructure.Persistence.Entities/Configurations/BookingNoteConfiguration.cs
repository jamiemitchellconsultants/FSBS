using FSBS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSBS.Infrastructure.Persistence.Entities.Configurations;

public class BookingNoteConfiguration : IEntityTypeConfiguration<BookingNote>
{
    public void Configure(EntityTypeBuilder<BookingNote> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("note_id");

        builder.Property(e => e.AuthorId).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content").IsRequired().HasColumnType("text");
        builder.Property(e => e.IsInternal).IsRequired().HasDefaultValue(true);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
    }
}
