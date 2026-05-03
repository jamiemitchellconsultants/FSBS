namespace FSBS.Domain.Entities;

/// <summary>
/// An internal note attached to a <see cref="Booking"/> by a staff member.
/// Notes are visible only to staff and are used to record context such as
/// special requirements, customer communications, or operational concerns.
/// </summary>
public class BookingNote : AuditableEntity, ISoftDeletable
{
    /// <summary>The booking this note belongs to.</summary>
    public Guid BookingId { get; set; }

    /// <summary><see cref="AppUser.Id"/> of the staff member who wrote the note.</summary>
    public Guid AuthorId { get; set; }

    /// <summary>The note content. Plain text; displayed in the booking detail view.</summary>
    public string Body { get; set; } = string.Empty;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the parent booking.</summary>
    public Booking Booking { get; set; } = null!;
}
