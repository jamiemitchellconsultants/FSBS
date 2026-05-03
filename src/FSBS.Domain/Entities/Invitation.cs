using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// An invitation to register on the FSBS platform as a corporate customer.
/// Issued by SalesStaff (for <see cref="InviteeRole.CorporateManager"/>) or
/// by a CorporateManager (for <see cref="InviteeRole.CorporateStudent"/>) within
/// their own organisation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Token security:</b> the raw invitation token is a cryptographically random
/// value emailed to the invitee. Only the SHA-256 hex digest is stored in
/// <see cref="TokenHash"/>; the raw token is never persisted. The Pre Sign-up
/// Lambda re-hashes the presented token and compares against this column.
/// </para>
/// <para>
/// <b>Uniqueness:</b> a partial unique index on <c>(invitee_email, org_id)</c>
/// where <c>status = 'Pending'</c> ensures only one active invitation exists per
/// address per organisation, while still permitting re-invitation after expiry or
/// revocation.
/// </para>
/// <para>
/// <b>Expiry:</b> invitations expire after 7 days by default. A nightly Lambda
/// sweeps for expired rows and sets their status to <c>Expired</c>.
/// </para>
/// </remarks>
public class Invitation : AuditableEntity
{
    /// <summary>The organisation the invitee will join on registration.</summary>
    public Guid OrgId { get; set; }

    /// <summary>Email address of the person being invited.</summary>
    public string InviteeEmail { get; set; } = string.Empty;

    /// <summary>
    /// The role the invitee will receive within the organisation on registration.
    /// A CorporateManager may only issue <see cref="InviteeRole.CorporateStudent"/>
    /// invitations and only for their own org.
    /// </summary>
    public InviteeRole InviteeRole { get; set; }

    /// <summary>
    /// SHA-256 hex digest of the raw invitation token. Fixed-length 64-character
    /// string stored in a <c>char(64)</c> column with a unique constraint.
    /// The raw token is delivered solely via email and never stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the invitation: <c>Pending</c> (awaiting registration),
    /// <c>Claimed</c> (registration complete), <c>Expired</c> (7-day window
    /// elapsed), or <c>Revoked</c> (cancelled by an authorised user).
    /// </summary>
    public InvitationStatus Status { get; set; }

    /// <summary>UTC timestamp after which this invitation can no longer be used.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the newly registered user who claimed this
    /// invitation. Populated only when <see cref="Status"/> is <c>Claimed</c>.
    /// </summary>
    public Guid? ClaimedBy { get; set; }

    /// <summary>UTC timestamp at which the invitation was claimed.</summary>
    public DateTimeOffset? ClaimedAt { get; set; }

    /// <summary>
    /// <see cref="AppUser.Id"/> of the user who revoked this invitation.
    /// Populated only when <see cref="Status"/> is <c>Revoked</c>.
    /// </summary>
    public Guid? RevokedBy { get; set; }

    /// <summary>UTC timestamp at which the invitation was revoked.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Navigation to the target organisation.</summary>
    public Organisation Organisation { get; set; } = null!;
}
