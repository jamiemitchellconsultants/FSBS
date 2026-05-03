using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// Core identity record that bridges a Cognito user account with the FSBS
/// domain model. Every authenticated user — staff and customer alike — has
/// exactly one <c>AppUser</c> row.
/// </summary>
/// <remarks>
/// Two distinct Cognito pools feed this table:
/// <list type="bullet">
///   <item><b>Staff pool</b> — users federated from Microsoft Entra ID. The
///     Post-Confirmation Lambda creates the <c>AppUser</c> row and assigns the
///     <c>AppRole</c> by mapping the user's Entra group membership.</item>
///   <item><b>Customer pool</b> — private customers and corporate users registered
///     via invitation. The <c>AppRole</c> is carried in the invitation and written
///     by the same Post-Confirmation Lambda.</item>
/// </list>
/// The record is tenant-scoped so that staff (root tenant) and customers
/// (their organisation's tenant) are logically separated at query time.
/// </remarks>
public class AppUser : AuditableEntity, ISoftDeletable, ITenantScoped
{
    /// <summary>
    /// Identifies the tenant this user belongs to. Staff always carry the
    /// school's root tenant ID; customers carry their organisation's tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The <c>sub</c> claim from the user's Cognito JWT. Immutable once set;
    /// used to look up the <c>AppUser</c> on every authenticated request.
    /// </summary>
    public string CognitoSub { get; set; } = string.Empty;

    /// <summary>
    /// The user's email address as held in Cognito. For staff this originates
    /// from Entra ID; for customers it is the address used during registration.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The user's primary application role, which drives all authorization
    /// policy decisions. Set at registration and updated by the Token Refresh
    /// Lambda when an Entra group change is detected for staff users.
    /// </summary>
    public AppRole AppRole { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <summary>Personal details for this user.</summary>
    public UserProfile? Profile { get; set; }

    /// <summary>
    /// Corporate organisation memberships. A user may belong to at most one
    /// active organisation in the current implementation, but the schema
    /// allows for future multi-org support.
    /// </summary>
    public ICollection<OrgMembership> OrgMemberships { get; set; } = [];
}
