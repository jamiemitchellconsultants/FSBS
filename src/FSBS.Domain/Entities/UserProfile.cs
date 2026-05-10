namespace FSBS.Domain.Entities;

/// <summary>
/// Mutable personal details for an <see cref="AppUser"/>. Stored in a
/// separate table that shares the same primary key column (<c>user_id</c>)
/// as <c>app_users</c>, making this a strict 1-to-1 optional extension row.
/// </summary>
/// <remarks>
/// The shared PK means no separate FK column exists — the join is always
/// <c>user_profiles.user_id = app_users.user_id</c>. EF Core models this
/// via <c>HasForeignKey&lt;UserProfile&gt;(p =&gt; p.Id)</c> in
/// <c>AppUserConfiguration</c>.
/// </remarks>
public class UserProfile : AuditableEntity
{
    /// <summary>Legal first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Legal last name / surname.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Optional contact phone number. Used for booking reminder SMS
    /// notifications when the user has opted in.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Date of birth. Used for identity verification purposes.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Pilot or cabin-crew licence number issued by the regulatory authority.</summary>
    public string? LicenceNumber { get; set; }

    /// <summary>Expiry date of the licence. Null if the licence does not expire or is not yet recorded.</summary>
    public DateOnly? LicenceExpiry { get; set; }

    /// <summary>S3 object key for the user's profile photo. Null until a photo has been uploaded.</summary>
    public string? PhotoS3Key { get; set; }

    /// <summary>The <see cref="AppUser"/> this profile belongs to.</summary>
    public AppUser User { get; set; } = null!;
}
