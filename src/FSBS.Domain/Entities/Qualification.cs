namespace FSBS.Domain.Entities;

/// <summary>
/// A regulatory or professional qualification held by a specific user
/// (e.g. ATPL, Type Rating, SEP). Records issue and expiry dates and links
/// to the supporting document stored in S3.
/// </summary>
/// <remarks>
/// The <c>QualificationExpiring</c> notification event is dispatched 30 and 7 days
/// before <see cref="ExpiryDate"/> so that the relevant instructor or course
/// director can arrange renewal.
/// </remarks>
public class Qualification : AuditableEntity, ISoftDeletable
{
    /// <summary>The user (typically an instructor) who holds this qualification.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Qualification type code displayed in the UI and on instructor profiles
    /// (e.g. "ATPL(A)", "B737 Type Rating", "SEP").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The date on which the qualification was issued.</summary>
    public DateOnly IssuedDate { get; set; }

    /// <summary>
    /// The date on which the qualification expires.
    /// <c>null</c> for qualifications with no expiry (e.g. academic degrees).
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>
    /// S3 object key for the supporting document (certificate, licence scan)
    /// stored in the <c>fsbs-documents</c> bucket. Served via pre-signed URL only.
    /// </summary>
    public string? DocumentS3Key { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the staff member who verified this qualification.
    /// <c>null</c> until verified.
    /// </summary>
    public Guid? VerifiedBy { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation to the user who holds this qualification.</summary>
    public AppUser User { get; set; } = null!;
}
