using FSBS.Domain.Enums;

namespace FSBS.Domain.Entities;

/// <summary>
/// A corporate customer entity — typically an airline, charter operator, or
/// training institution — that books simulator sessions on behalf of its staff.
/// </summary>
/// <remarks>
/// Every <c>Organisation</c> has exactly one <see cref="OrgAccount"/> (1-to-1)
/// that tracks the financial relationship with the school. Members are linked
/// via <see cref="OrgMembership"/> rows and carry either the
/// <see cref="OrgRole.Manager"/> or <see cref="OrgRole.Student"/> role within
/// the org. The <see cref="CustomerClass"/> determines which
/// <see cref="PricingPolicy"/> tier applies to bookings made under this org.
/// Row-level security in PostgreSQL enforces that each org's data is visible
/// only to its own tenant.
/// </remarks>
public class Organisation : AuditableEntity, ISoftDeletable, ITenantScoped
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display name of the organisation as it appears on invoices and the
    /// management dashboard (e.g. "Sunrise Airlines Ltd").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Pricing classification that selects the applicable
    /// <see cref="PricingPolicy"/> for this organisation's bookings.
    /// Corporate organisations typically receive negotiated rates configured
    /// on the relevant <see cref="DiscountRule"/> rows.
    /// </summary>
    public CustomerClass CustomerClass { get; set; }

    /// <summary>
    /// Optional contract type descriptor (e.g. "Annual", "Ad-hoc").
    /// Used for reporting and account management.
    /// </summary>
    public string? ContractType { get; set; }
    /// <summary>
    /// Maximum outstanding balance (GBP) the school will extend to this
    /// organisation before blocking new bookings. Mirrors the value on
    /// <see cref="OrgAccount.CreditLimitGbp"/> for quick access.
    /// </summary>
    public decimal CreditLimitGbp { get; set; }
    /// <summary>Primary billing email address for invoices and account notifications.</summary>
    public string BillingEmail { get; set; } = string.Empty;
    /// <summary>Whether this organisation is currently active and able to make bookings.</summary>
    public bool IsActive { get; set; } = true;
    /// <inheritdoc/>
    public bool IsDeleted { get; set; }
    /// <summary>The financial account held for this organisation.</summary>
    public OrgAccount? Account { get; set; }

    /// <summary>All staff members linked to this organisation.</summary>
    public ICollection<OrgMembership> Memberships { get; set; } = [];

    /// <summary>
    /// Pending, claimed, and historical invitation records issued to — or
    /// revoked for — users joining this organisation.
    /// </summary>
    public ICollection<Invitation> Invitations { get; set; } = [];
}
