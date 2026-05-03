namespace FSBS.Domain.Entities;

/// <summary>
/// Extends <see cref="EntityBase"/> with four audit columns that are stamped
/// automatically by <c>AuditInterceptor</c> on every write.
/// </summary>
/// <remarks>
/// <c>CreatedAt</c> and <c>CreatedBy</c> are set once on insert and never updated.
/// <c>UpdatedAt</c> and <c>UpdatedBy</c> are refreshed on every subsequent save.
/// All four columns are stored as <c>timestamptz</c> / <c>uuid</c> in the database.
/// Entities that should not be mutated after creation (e.g. <see cref="BookingDiscount"/>)
/// extend <see cref="EntityBase"/> directly rather than this type.
/// </remarks>
public abstract class AuditableEntity : EntityBase
{
    /// <summary>UTC timestamp of the row's initial insert.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC timestamp of the most recent update to this row.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the user whose action caused the insert.
    /// <c>null</c> for system-generated rows (e.g. seeded reference data).
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the user whose action caused the most recent update.
    /// <c>null</c> for system-generated rows.
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}
