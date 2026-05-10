namespace FSBS.Domain.Entities;

/// <summary>
/// Reference table defining the lifecycle statuses for organisation accounts.
/// </summary>
public class AccountStatusRef
{
    /// <summary>Short code used as the FK value in <see cref="OrgAccount"/>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable display label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether this status is available for selection.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether accounts in this status can place new bookings.</summary>
    public bool AllowsBooking { get; set; } = true;
}
