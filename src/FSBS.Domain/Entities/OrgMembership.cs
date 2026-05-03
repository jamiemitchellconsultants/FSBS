using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Links an <see cref="AppUser"/> to an <see cref="Organisation"/> and captures
/// their role within that organisation (<see cref="OrgRole.Manager"/> or
/// <see cref="OrgRole.Student"/>).
/// </summary>
/// <remarks>
/// A unique constraint on <c>(user_id, org_id)</c> prevents a user from being
/// a member of the same organisation twice. The <c>OrgRole</c> here is distinct
/// from the user's <see cref="AppRole"/> — <c>AppRole</c> is the system-wide
/// authorization role, while <c>OrgRole</c> governs what the user can do
/// <em>within their corporate org</em> (e.g. a <c>CorporateManager</c> has
/// <see cref="OrgRole.Manager"/> and may issue student invitations for their org).
/// </remarks>
public class OrgMembership : AuditableEntity, ISoftDeletable
{
    /// <summary>The member user.</summary>
    public Guid UserId { get; set; }

    /// <summary>The organisation this membership belongs to.</summary>
    public Guid OrgId { get; set; }

    /// <summary>
    /// The user's functional role within the organisation. <c>Manager</c>
    /// permits booking on behalf of the org and issuing student invitations;
    /// <c>Student</c> permits booking for themselves at the org's negotiated rate.
    /// </summary>
    public OrgRole OrgRole { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Navigation property to the member user.</summary>
    public AppUser User { get; set; } = null!;

    /// <summary>Navigation property to the owning organisation.</summary>
    public Organisation Organisation { get; set; } = null!;
}
